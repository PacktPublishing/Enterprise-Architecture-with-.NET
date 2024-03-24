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

        builder.Services.AddTransient<CustomAuthorizationMessageHandler>();
        builder.Services
            .AddHttpClient("BooksAPI", client => client.BaseAddress = new Uri("http://localhost:5298"))
            .AddHttpMessageHandler<CustomAuthorizationMessageHandler>();
        builder.Services.AddHttpClient("NeedsNoAccessToken", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
        builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("BooksAPI"));
        builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("NeedsNoAccessToken"));

        builder.Services.AddOidcAuthentication(options =>
        {
            builder.Configuration.Bind("Local", options.ProviderOptions);
            // options.ProviderOptions.Authority = "http://localhost:8080/realms/demoeditor/";
            // options.ProviderOptions.ClientId = "Portal";
            options.UserOptions.RoleClaim = "realm_access.roles";
        });

        builder.Services.AddApiAuthorization().AddAccountClaimsPrincipalFactory<RolesClaimsPrincipalFactory>();

        await builder.Build().RunAsync();
    }
}
