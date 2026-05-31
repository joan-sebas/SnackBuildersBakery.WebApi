using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace Api.Auth;

public static class ApiKeyAuthenticationDefaults
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";
}

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SectionName = "Auth";

    public ApiKeyOptions ApiKeys { get; set; } = new();

    public AuthRoleOptions Roles { get; set; } = new();
}

public sealed class ApiKeyOptions
{
    public string? ManagerKey { get; set; }

    public string? PublicKey { get; set; }
}

public sealed class AuthRoleOptions
{
    public string? Manager { get; set; }

    public string? Public { get; set; }
}

public static class AuthenticationSetup
{
    public static IServiceCollection AddRoleAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthentication(ApiKeyAuthenticationDefaults.SchemeName)
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationDefaults.SchemeName,
                options => configuration.GetSection(ApiKeyAuthenticationOptions.SectionName).Bind(options));

        return services;
    }
}

internal sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IProblemDetailsService problemDetailsService)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var headerValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var apiKey = headerValues.Count == 1 ? headerValues[0] : headerValues.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
            return Task.FromResult(AuthenticateResult.Fail("API key is empty."));

        var role = ResolveRole(apiKey, Options);
        if (role is null)
            return Task.FromResult(AuthenticateResult.Fail("API key is invalid."));

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, role)
            ],
            Scheme.Name);

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        return WriteProblemDetailsAsync(
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "Authentication is required.");
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        return WriteProblemDetailsAsync(
            StatusCodes.Status403Forbidden,
            "Forbidden",
            "The authenticated principal is not allowed to access this resource.");
    }

    private static string? ResolveRole(string apiKey, ApiKeyAuthenticationOptions options)
    {
        if (MatchesConfiguredValue(apiKey, options.ApiKeys.ManagerKey) && !string.IsNullOrWhiteSpace(options.Roles.Manager))
            return options.Roles.Manager;

        if (MatchesConfiguredValue(apiKey, options.ApiKeys.PublicKey) && !string.IsNullOrWhiteSpace(options.Roles.Public))
            return options.Roles.Public;

        return null;
    }

    private static bool MatchesConfiguredValue(string candidate, string? configuredValue)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
            return false;

        var candidateBytes = Encoding.UTF8.GetBytes(candidate);
        var configuredBytes = Encoding.UTF8.GetBytes(configuredValue);
        return candidateBytes.Length == configuredBytes.Length
            && CryptographicOperations.FixedTimeEquals(candidateBytes, configuredBytes);
    }

    private Task WriteProblemDetailsAsync(int statusCode, string title, string detail)
    {
        if (Response.HasStarted)
            return Task.CompletedTask;

        Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Detail = detail,
            Status = statusCode
        };
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? Context.TraceIdentifier;

        return problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = Context,
            ProblemDetails = problemDetails
        }).AsTask();
    }
}
