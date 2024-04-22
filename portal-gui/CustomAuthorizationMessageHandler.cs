using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

public class CustomAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public CustomAuthorizationMessageHandler(IAccessTokenProvider provider, 
        NavigationManager navigation)
        : base(provider, navigation)
    {
        //ConfigureHandler(authorizedUrls: new[] { "http://books:81" }, scopes: new[] { "email", "profile" });
        ConfigureHandler(authorizedUrls: new[] { "http://books:81/Books/$count", "http://books:81/Books/" });
    }
}