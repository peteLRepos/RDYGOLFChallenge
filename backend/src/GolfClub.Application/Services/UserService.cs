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
        var user = new User(request.Name, request.Email, passwordHash, _dateTimeProvider.Now);

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

    public async Task<List<UserSearchResultDto>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < MinimumSearchQueryLength)
            return [];

        var users = await _users.SearchByNameAsync(query.Trim(), ct);
        return users.Select(u => new UserSearchResultDto(u.Id, u.Name)).ToList();
    }
}
