using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Harness.Modules.Auth.Domain;
using Harness.Modules.Auth.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Harness.UnitTests;

public class JwtTokenServiceTests
{
    private const string Secret = "test-secret-key-0123456789-abcdefghijklmnop";

    private static JwtTokenService CreateService(int expiryHours = 1)
        => new(Options.Create(new JwtOptions
        {
            Issuer = "test-iss",
            Audience = "test-aud",
            SecretKey = Secret,
            ExpiryHours = expiryHours
        }));

    private static AdminUser NewAdmin() => AdminUser.Create("admin", "hash", "Quản trị viên");

    [Fact]
    public void CreateToken_IncludesIdentityAndRolesClaims()
    {
        var token = CreateService().CreateToken(NewAdmin(), new[] { "Admin", "SuperAdmin" }).Token;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("admin", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal(AdminRoles.Admin, jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == AdminRoles.SuperAdmin);
        Assert.Equal("test-iss", jwt.Issuer);
        Assert.Equal("test-aud", jwt.Audiences.Single());
    }

    [Fact]
    public void CreateToken_ValidatesAgainstConfiguredParameters()
    {
        var result = CreateService().CreateToken(NewAdmin(), new[] { AdminRoles.Admin });

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = "test-iss",
            ValidateAudience = true, ValidAudience = "test-aud",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(result.Token, parameters, out _);

        Assert.Equal("admin", principal.Identity?.Name);
        Assert.True(principal.IsInRole(AdminRoles.Admin));
    }

    [Fact]
    public void CreateToken_ExpiresPerConfiguredHours()
    {
        var result = CreateService(expiryHours: 2).CreateToken(NewAdmin(), new[] { AdminRoles.Admin });

        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow.AddHours(1.5));
        Assert.True(result.ExpiresAt < DateTimeOffset.UtcNow.AddHours(2.5));
    }

    [Fact]
    public void PasswordHashHelper_RoundTrips()
    {
        var hash = PasswordHashHelper.Hash("Harness@123");
        Assert.True(PasswordHashHelper.Verify("Harness@123", hash));
        Assert.False(PasswordHashHelper.Verify("wrong-password", hash));
    }
}