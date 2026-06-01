using Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Api.OpenApi;

/// <summary>
/// Declares the <c>X-Api-Key</c> security scheme on the OpenAPI document and attaches the
/// requirement to operations whose endpoint carries an authorization policy. This makes Scalar
/// render an API-key input and a lock on manager-only routes, while anonymous routes stay open.
/// </summary>
internal sealed class ApiKeySecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeId = ApiKeyAuthenticationDefaults.SchemeName;

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeId] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = ApiKeyAuthenticationDefaults.HeaderName,
            Description = "Manager API key. Required only for menu mutations and kitchen monitoring."
        };

        var protectedRoutes = ProtectedRoutes(context);

        foreach (var (pathKey, pathItem) in document.Paths)
        {
            if (pathItem.Operations is null)
            {
                continue;
            }

            var relative = pathKey.TrimStart('/');
            foreach (var (method, operation) in pathItem.Operations)
            {
                if (!protectedRoutes.Contains((relative, method.ToString().ToUpperInvariant())))
                {
                    continue;
                }

                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(SchemeId, document)] = []
                });
            }
        }

        return Task.CompletedTask;
    }

    private static HashSet<(string Path, string Method)> ProtectedRoutes(
        OpenApiDocumentTransformerContext context) =>
        context.DescriptionGroups
            .SelectMany(group => group.Items)
            .Where(description => description.ActionDescriptor.EndpointMetadata
                .OfType<IAuthorizeData>()
                .Any())
            .Select(description => (
                description.RelativePath?.TrimStart('/') ?? string.Empty,
                (description.HttpMethod ?? string.Empty).ToUpperInvariant()))
            .ToHashSet();
}
