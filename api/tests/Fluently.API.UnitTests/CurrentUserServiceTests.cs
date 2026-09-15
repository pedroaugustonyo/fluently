using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Fluently.API.Exceptions;
using Fluently.API.Services;

using Microsoft.AspNetCore.Http;

using Moq;

namespace Fluently.API.UnitTests;

public sealed class CurrentUserServiceTests
{
    [Fact]
    public void GetUserId_SubjectClaimExists_ReturnsUserId()
    {
        var userId = Guid.NewGuid();
        var accessor = CreateAccessor(
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()));
        var service = new CurrentUserService(accessor.Object);

        var result = service.GetUserId();

        Assert.Equal(userId, result);
    }

    [Fact]
    public void GetUserId_NameIdentifierFallbackExists_ReturnsUserId()
    {
        var userId = Guid.NewGuid();
        var accessor = CreateAccessor(
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        var service = new CurrentUserService(accessor.Object);

        var result = service.GetUserId();

        Assert.Equal(userId, result);
    }

    [Fact]
    public void GetUserId_HttpContextMissing_ThrowsLocalizedUnauthorizedException()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(candidate => candidate.HttpContext).Returns((HttpContext?)null);
        var service = new CurrentUserService(accessor.Object);

        var exception = Assert.Throws<UnauthorizedException>(() => service.GetUserId());

        Assert.Equal("Não foi possível identificar o usuário autenticado.", exception.Detail);
    }

    [Fact]
    public void GetUserId_IdentityClaimMissing_ThrowsLocalizedUnauthorizedException()
    {
        var accessor = CreateAccessor(new Claim(ClaimTypes.Email, "pedro@example.com"));
        var service = new CurrentUserService(accessor.Object);

        var exception = Assert.Throws<UnauthorizedException>(() => service.GetUserId());

        Assert.Equal("Não foi possível identificar o usuário autenticado.", exception.Detail);
    }

    [Fact]
    public void GetUserId_IdentityClaimMalformed_ThrowsLocalizedUnauthorizedException()
    {
        var accessor = CreateAccessor(
            new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid"));
        var service = new CurrentUserService(accessor.Object);

        var exception = Assert.Throws<UnauthorizedException>(() => service.GetUserId());

        Assert.Equal("Não foi possível identificar o usuário autenticado.", exception.Detail);
    }

    private static Mock<IHttpContextAccessor> CreateAccessor(params Claim[] claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(candidate => candidate.HttpContext).Returns(context);

        return accessor;
    }
}
