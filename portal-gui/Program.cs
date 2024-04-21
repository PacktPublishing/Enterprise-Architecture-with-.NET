using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace portal_gui;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:82") });

        builder.Services.AddOidcAuthentication(options =>
        {
            builder.Configuration.Bind("Local", options.ProviderOptions);
            options.ProviderOptions.Authority = "http://localhost:8080/realms/demoeditor/";
            options.ProviderOptions.ClientId = "Portal";
            options.UserOptions.RoleClaim = "realm_access.roles";
        });

        builder.Services.AddApiAuthorization().AddAccountClaimsPrincipalFactory<RolesClaimsPrincipalFactory>();

        await builder.Build().RunAsync();
    }
}
