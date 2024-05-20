using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

public class EditorRequirement : IAuthorizationRequirement
{
    public EditorRequirement()
    {
    }
}

public class EditorAuthorizationHandler : AuthorizationHandler<EditorRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        EditorRequirement requirement)
    {
        foreach (var role in context.User.Claims.Where(claim => claim.Type == "realm_roles"))
        {
            if (role.Value == "editor") context.Succeed(requirement);
            if (role.Value == "director") context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}

public class EditorApiKeyRequirement : IAuthorizationRequirement
{
    public EditorApiKeyRequirement()
    {
    }
}

public class EditorApiKeyAuthorizationHandler : AuthorizationHandler<EditorApiKeyRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly IApiKeyValidation _apiKeyValidation;

    public EditorApiKeyAuthorizationHandler(IHttpContextAccessor httpContextAccessor, IApiKeyValidation apiKeyValidation)
    {
        _httpContextAccessor = httpContextAccessor;
        _apiKeyValidation = apiKeyValidation;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        EditorApiKeyRequirement requirement)
    {
        foreach (var role in context.User.Claims.Where(claim => claim.Type == "realm_roles"))
        {
            if (role.Value == "editor") context.Succeed(requirement);
            if (role.Value == "director") context.Succeed(requirement);
        }

        string apiKey = _httpContextAccessor?.HttpContext?.Request.Headers["X-API-Key"].ToString();
        if (!string.IsNullOrWhiteSpace(apiKey) && _apiKeyValidation.IsValidApiKey(apiKey)) context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
