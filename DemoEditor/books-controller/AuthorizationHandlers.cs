using Microsoft.AspNetCore.Authorization;

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
        foreach (var role in context.User.Claims.Where(claim => claim.Type == "user_roles"))
        {
            if (role.Value == "editor") context.Succeed(requirement);
            if (role.Value == "director") context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
