using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Api.Auth;

public static class ApiAuthorizationPolicies
{
    public const string Manager = "Manager";
    public const string Public = "Public";
}

public static class AuthorizationSetup
{
    public static IServiceCollection AddRoleAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, ConfiguredRoleAuthorizationHandler>();
        services.AddAuthorizationBuilder()
            .AddPolicy(
                ApiAuthorizationPolicies.Manager,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ConfiguredRoleRequirement(ConfiguredRole.Manager)))
            .AddPolicy(
                ApiAuthorizationPolicies.Public,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new ConfiguredRoleRequirement(ConfiguredRole.Public, ConfiguredRole.Manager)));

        return services;
    }

    public static TBuilder RequireManager<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(ApiAuthorizationPolicies.Manager);
        return builder;
    }

    public static TBuilder RequirePublic<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.RequireAuthorization(ApiAuthorizationPolicies.Public);
        return builder;
    }
}

internal enum ConfiguredRole
{
    Manager,
    Public
}

internal sealed class ConfiguredRoleRequirement(params ConfiguredRole[] roles) : IAuthorizationRequirement
{
    public IReadOnlyCollection<ConfiguredRole> Roles { get; } = roles;
}

internal sealed class ConfiguredRoleAuthorizationHandler(IOptionsMonitor<ApiKeyAuthenticationOptions> options)
    : AuthorizationHandler<ConfiguredRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ConfiguredRoleRequirement requirement)
    {
        var configuredRoles = options.Get(ApiKeyAuthenticationDefaults.SchemeName).Roles;

        foreach (var role in requirement.Roles)
        {
            if (IsInConfiguredRole(context.User, ResolveRole(configuredRoles, role)))
            {
                context.Succeed(requirement);
                break;
            }
        }

        return Task.CompletedTask;
    }

    private static string? ResolveRole(AuthRoleOptions roles, ConfiguredRole role)
    {
        return role switch
        {
            ConfiguredRole.Manager => roles.Manager,
            ConfiguredRole.Public => roles.Public,
            _ => null
        };
    }

    private static bool IsInConfiguredRole(ClaimsPrincipal user, string? role)
    {
        return !string.IsNullOrWhiteSpace(role) && user.IsInRole(role);
    }
}
