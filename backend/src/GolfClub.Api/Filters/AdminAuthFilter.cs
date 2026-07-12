using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GolfClub.Api.Filters;

/// <summary>
/// Placeholder admin gate for this assignment's scope — a shared header key, not real auth.
/// In production this would be replaced by ASP.NET Identity/JWT with per-admin accounts.
/// </summary>
public class AdminAuthFilter : IActionFilter
{
    private const string HeaderName = "X-Admin-Key";
    private readonly IConfiguration _configuration;

    public AdminAuthFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var expectedKey = _configuration["Admin:ApiKey"];
        var providedKey = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Missing or invalid admin key." });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
