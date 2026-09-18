using System.IdentityModel.Tokens.Jwt;
using Fluently.API.Options;
using Fluently.API.Services;

namespace Fluently.API.UnitTests;

public sealed class JwtTokenServiceTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "Fluently.API",
        Audience = "Fluently.Client",
        SigningKey = "unit-test-signing-key-with-at-least-32-characters",
        ExpirationMinutes = 60,
    };

    [Fact]
    public void Create_ValidUser_ContainsOnlySubjectAndEmailClaims()
    {
        var user = TestData.CreateUser();
        var service = new JwtTokenService(
            Microsoft.Extensions.Options.Options.Create(Options),
            new FixedTimeProvider(TestData.Now)
        );

        var result = service.Create(user.Id, user.Email);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        var identityClaims = token.Claims.Where(claim => claim.Type is "sub" or "email").ToArray();

        Assert.Equal(2, identityClaims.Length);
        Assert.Equal(user.Id.ToString(), token.Subject);
        Assert.Equal(user.Email, token.Claims.Single(claim => claim.Type == "email").Value);
        Assert.DoesNotContain("tenant_id", token.Claims.Select(claim => claim.Type));
        Assert.DoesNotContain("role", token.Claims.Select(claim => claim.Type));
        Assert.DoesNotContain("permission", token.Claims.Select(claim => claim.Type));
    }

    [Fact]
    public void Create_ValidUser_ExpiresExactlyAfterConfiguredMinutes()
    {
        var service = new JwtTokenService(
            Microsoft.Extensions.Options.Options.Create(Options),
            new FixedTimeProvider(TestData.Now)
        );

        var result = service.Create(Guid.NewGuid(), "pedro@example.com");

        Assert.Equal(TestData.Now.AddMinutes(60), result.ExpiresAt);
    }

    [Fact]
    public void Create_ValidUser_UsesConfiguredIssuerAudienceAndHmacSha256()
    {
        var service = new JwtTokenService(
            Microsoft.Extensions.Options.Options.Create(Options),
            new FixedTimeProvider(TestData.Now)
        );

        var result = service.Create(Guid.NewGuid(), "pedro@example.com");
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        Assert.Equal(Options.Issuer, token.Issuer);
        Assert.Contains(Options.Audience, token.Audiences);
        Assert.Equal("HS256", token.Header.Alg);
    }
}
