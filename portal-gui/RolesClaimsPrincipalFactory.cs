using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using System.Security.Claims;
using System.Text.Json;

namespace portal_gui
{
    public class RolesClaimsPrincipalFactory : AccountClaimsPrincipalFactory<RemoteUserAccount>
    {
        public RolesClaimsPrincipalFactory(IAccessTokenProviderAccessor accessor) : base(accessor)
        {
        }

        public override async ValueTask<ClaimsPrincipal> CreateUserAsync(RemoteUserAccount account, RemoteAuthenticationUserOptions options)
        {
            var user = await base.CreateUserAsync(account, options);
            if (user?.Identity != null && user.Identity.IsAuthenticated)
            {
                var identity = (ClaimsIdentity)user.Identity;
                var resourceaccess = identity.FindAll("realm_access");
                string Contenu = resourceaccess.First().Value;
                JsonElement elem = JsonDocument.Parse(Contenu).RootElement;
                foreach (JsonElement role in elem.GetProperty("roles").EnumerateArray())
                {
                    identity.AddClaim(new Claim(options.RoleClaim, role.GetString() ?? String.Empty));
                    if (role.GetString() == "director")
                        identity.AddClaim(new Claim(options.RoleClaim, "editor"));
                }
            }
            return user;
        }
    }
}
