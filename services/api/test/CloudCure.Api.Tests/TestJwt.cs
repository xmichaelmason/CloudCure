using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CloudCure.Api.Auth;
using Microsoft.IdentityModel.Tokens;

namespace CloudCure.Api.Tests;

public static class TestJwt
{
    public static string Mint(Guid? personId = null, params string[] roles)
    {
        var claims = new List<Claim>();
        if (personId is { } id)
        {
            claims.Add(new Claim(InternalJwt.SubjectClaimType, id.ToString()));
        }
        claims.AddRange(roles.Select(r => new Claim(InternalJwt.RoleClaimType, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(PostgresApiFactory.JwtSharedSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: InternalJwt.Issuer,
            audience: InternalJwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(60),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
