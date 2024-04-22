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

        builder.Services.AddScoped(sp => new HttpClient());

        // builder.Services.AddOidcAuthentication(options =>
        // {
        //     builder.Configuration.Bind("Local", options.ProviderOptions);
        //     // These two lines are commented out because the line above gets the values from the .json configuration
        //     // options.ProviderOptions.Authority = "http://iam:8080/realms/demoeditor/";
        //     // options.ProviderOptions.ClientId = "portal";
        //     options.UserOptions.RoleClaim = "realm_access.roles";
        // });

        // // This is used for the roles to be taken into account when displaying some menus or allowing access to pages
        // builder.Services.AddApiAuthorization().AddAccountClaimsPrincipalFactory<RolesClaimsPrincipalFactory>();

        await builder.Build().RunAsync();
    }
}
