using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace portal_gui;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services
            .AddHttpClient("BooksAPI", client => client.BaseAddress = new Uri("http://books:81/Books/"))
            .AddHttpMessageHandler(x => {
                var handler = x.GetRequiredService<AuthorizationMessageHandler>()
                    .ConfigureHandler(new[] { "http://books:81" });
                return handler;
            });

        builder.Services
            .AddHttpClient("AuthorsAPI", client => client.BaseAddress = new Uri("http://authors:82/Authors/"))
            .AddHttpMessageHandler(x => {
                var handler = x.GetRequiredService<AuthorizationMessageHandler>()
                    .ConfigureHandler(new[] { "http://authors:82" });
                return handler;
            });

        builder.Services.AddOidcAuthentication(options =>
        {
            options.ProviderOptions.Authority = "http://iam:8080/realms/demoeditor/";
            options.ProviderOptions.ClientId = "portal";
            options.ProviderOptions.ResponseType = "code";
            options.UserOptions.RoleClaim = "realm_access.roles";
        });

        builder.Services.AddApiAuthorization().AddAccountClaimsPrincipalFactory<RolesClaimsPrincipalFactory>();

        await builder.Build().RunAsync();
    }
}
