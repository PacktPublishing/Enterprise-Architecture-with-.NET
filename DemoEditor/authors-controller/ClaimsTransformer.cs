using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

public class ClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ClaimsIdentity claimsIdentity = (ClaimsIdentity)principal.Identity;
        if (claimsIdentity.IsAuthenticated)
        {
            foreach (var c in claimsIdentity.Clone().FindAll((claim) => claim.Type == "realm_access"))
            {
                JsonDocument doc = JsonDocument.Parse(c.Value);
                foreach (JsonElement elem in doc.RootElement.GetProperty("roles").EnumerateArray())
                    claimsIdentity.AddClaim(new Claim("realm_roles", elem.GetString() ?? String.Empty));
            }
        }
        return Task.FromResult(principal);
    }
}