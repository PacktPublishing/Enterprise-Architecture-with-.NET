using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace books_controller
{
    /// <summary>
    /// Reference: https://docs.microsoft.com/fr-fr/aspnet/core/security/authentication/claims?view=aspnetcore-6.0
    /// </summary>
    public class ClaimsTransformer : IClaimsTransformation
    {
        //private string PrefixeRoleClaim { get; init; }
        //private string OIDCClientId { get; init; }
        //private string SuffixeRoleClaim { get; init; }
        //private string TargetUserRolesClaimName { get; init; }

        // public ClaimsTransformer(IConfiguration config)
        // {
        //     //string ModelePourRoleClaim = "resource_access.BooksAPI.roles";
        //     //PrefixeRoleClaim = ModelePourRoleClaim.Substring(0, ModelePourRoleClaim.IndexOf("."));
        //     //SuffixeRoleClaim = ModelePourRoleClaim.Substring(ModelePourRoleClaim.LastIndexOf(".") + 1);
        //     //OIDCClientId = "BooksAPI";
        //     //TargetUserRolesClaimName = "user_roles";
        // }

        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)principal.Identity;
            if (claimsIdentity.IsAuthenticated)
            {
                foreach (var c in claimsIdentity.Clone().FindAll((claim) => claim.Type == "realm_access"))
                {
                    JsonElement elem = JsonDocument.Parse(c.Value).RootElement;
                    foreach (JsonElement role in elem.GetProperty("roles").EnumerateArray())
                        claimsIdentity.AddClaim(new Claim("user_roles", role.GetString() ?? String.Empty));
                }
            }
            return Task.FromResult(principal);
        }
    }
}
