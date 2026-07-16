using System.Security.Cryptography;
using GolfClub.Application.DTOs;
using GolfClub.Application.Exceptions;
using GolfClub.Application.Interfaces;
using GolfClub.Domain.Entities;
using GolfClub.Domain.Exceptions;

namespace GolfClub.Application.Services;

public class UserService : IUserService
{
    private const int MinimumSearchQueryLength = 2;
    private const int MinimumPasswordLength = 8;
    private const int GeneratedPasswordLength = 12;

    // Excludes visually ambiguous characters (0/O, 1/I/l) since this is read off a screen and
    // retyped by hand — no email step to copy-paste it from (see README).
    private const string GeneratedPasswordAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";

    // Arbitrary fixed identifier for the "at least one active admin must remain" invariant —
    // see EnsureNotLastActiveAdminAsync. Any value works as long as it's unique within this
    // codebase's set of advisory locks (there's only this one so far).
    private const long AdminGuardLockId = 727100;

    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UserService(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IDateTimeProvider dateTimeProvider)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserRequest request, CancellationToken ct = default)
    {
        // The hash produced by IPasswordHasher is never empty, even for an empty input — so this
        // has to be checked against the raw password here, not left to User's passwordHash check.
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < MinimumPasswordLength)
            throw new DomainException($"Password must be at least {MinimumPasswordLength} characters.");

        // Must match the normalization User's constructor applies, or this check would miss
        // an existing account that only differs by casing.
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existing = await _users.GetByEmailAsync(normalizedEmail, ct);
        if (existing is not null)
            throw new DomainException("A user with this email already exists.");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = new User(request.Name, request.Email, passwordHash, _dateTimeProvider.Now, request.Handicap);

        await _users.AddAsync(user, ct);
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConflictException)
        {
            // The GetByEmailAsync check above raced with a concurrent registration of the same
            // email — the DB's unique index caught it. Report it identically to the synchronous
            // check above, so the caller sees the same 400 either way, not a raw 500.
            throw new DomainException("A user with this email already exists.");
        }

        var token = _tokenGenerator.GenerateToken(user);
        return new AuthResponseDto(token, UserDto.FromEntity(user));
    }

    public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        // Always returns 200 rather than a 404 for an unknown email — see ForgotPasswordResponseDto.
        // Tracked lookup, not GetByEmailAsync: ResetPassword below mutates this entity, and an
        // untracked one would let SaveChangesAsync silently no-op instead of persisting the reset.
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _users.GetTrackedByEmailAsync(normalizedEmail, ct);
        if (user is null)
            return new ForgotPasswordResponseDto(AccountFound: false, NewPassword: null);

        var newPassword = GenerateRandomPassword();
        user.ResetPassword(_passwordHasher.Hash(newPassword));
        await _unitOfWork.SaveChangesAsync(ct);

        return new ForgotPasswordResponseDto(AccountFound: true, newPassword);
    }

    private static string GenerateRandomPassword()
    {
        var chars = new char[GeneratedPasswordLength];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = GeneratedPasswordAlphabet[RandomNumberGenerator.GetInt32(GeneratedPasswordAlphabet.Length)];

        return new string(chars);
    }

    public async Task<List<UserSearchResultDto>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < MinimumSearchQueryLength)
            return [];

        var users = await _users.SearchByNameAsync(query.Trim(), ct);
        return users.Select(u => new UserSearchResultDto(u.Id, u.Name, u.Handicap)).ToList();
    }

    public async Task<List<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await _users.GetAllAsync(ct);
        return users.Select(UserDto.FromEntity).ToList();
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"User '{id}' was not found.");
        return UserDto.FromEntity(user);
    }

    public Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default) =>
        // Wrapped in a transaction + advisory lock, not just the guard check below: without it,
        // two concurrent requests could each pass EnsureNotLastActiveAdminAsync's read before
        // either commits, both proceed, and leave zero active admins — the exact scenario this
        // guard exists to prevent. The lock forces the second request to wait, re-read, and
        // correctly fail once the first has committed. See UnitOfWork.AcquireExclusiveLockAsync.
        _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.AcquireExclusiveLockAsync(AdminGuardLockId, ct);

            var user = await _users.GetByIdAsync(id, ct)
                ?? throw new NotFoundException($"User '{id}' was not found.");

            if (!isActive && user.IsAdmin)
                await EnsureNotLastActiveAdminAsync(user, ct);

            if (isActive)
                user.Activate();
            else
                user.Deactivate();

            await _unitOfWork.SaveChangesAsync(ct);
        }, ct);

    public Task SetAdminAsync(Guid id, bool isAdmin, CancellationToken ct = default) =>
        // Same race-condition rationale as SetActiveAsync above.
        _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.AcquireExclusiveLockAsync(AdminGuardLockId, ct);

            var user = await _users.GetByIdAsync(id, ct)
                ?? throw new NotFoundException($"User '{id}' was not found.");

            if (!isAdmin && user.IsAdmin)
                await EnsureNotLastActiveAdminAsync(user, ct);

            if (isAdmin)
                user.PromoteToAdmin();
            else
                user.DemoteFromAdmin();

            await _unitOfWork.SaveChangesAsync(ct);
        }, ct);

    /// <summary>
    /// Guards against demoting/deactivating the only remaining admin, which would permanently lock
    /// everyone out of the admin endpoints (no "break glass" recovery path exists in this scope).
    /// Must only be called while holding the AdminGuardLockId lock (see callers above).
    /// </summary>
    private async Task EnsureNotLastActiveAdminAsync(User user, CancellationToken ct)
    {
        var allUsers = await _users.GetAllAsync(ct);
        var hasOtherActiveAdmin = allUsers.Any(u => u.Id != user.Id && u.IsAdmin && u.IsActive);

        if (!hasOtherActiveAdmin)
            throw new DomainException("Cannot remove the last remaining admin.");
    }
}
