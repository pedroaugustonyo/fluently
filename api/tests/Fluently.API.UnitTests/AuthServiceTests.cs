using Fluently.API.DTOs.Auth;
using Fluently.API.DTOs.Users;
using Fluently.API.Exceptions;
using Fluently.API.Models;
using Fluently.API.Repositories;
using Fluently.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fluently.API.UnitTests;

public sealed class AuthServiceTests
{
    private readonly Mock<IUserRepository> userRepository = new();
    private readonly Mock<IPasswordHasher<UserModel>> passwordHasher = new();
    private readonly Mock<ITokenService> tokenService = new();
    private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;

    [Fact]
    public async Task RegisterAsync_ValidRequest_PersistsHashedUserAndReturnsUserWithoutToken()
    {
        var request = CreateRegisterRequest();
        UserModel? addedUser = null;
        userRepository
            .Setup(repository => repository.AddAsync(It.IsAny<UserModel>(), cancellationToken))
            .Callback<UserModel, CancellationToken>((user, _) => addedUser = user)
            .Returns(Task.CompletedTask);
        userRepository.Setup(repository => repository.SaveChangesAsync(cancellationToken)).ReturnsAsync(1);
        passwordHasher
            .Setup(hasher => hasher.HashPassword(It.IsAny<UserModel>(), request.Password))
            .Returns("secure-hash");
        var service = CreateService();

        var response = await service.RegisterAsync(request, cancellationToken);

        Assert.NotNull(addedUser);
        Assert.Equal("secure-hash", addedUser.PasswordHash);
        Assert.NotEqual(request.Password, addedUser.PasswordHash);
        Assert.Equal(request.FirstName, response.FirstName);
        Assert.Equal(request.LastName, response.LastName);
        Assert.Equal(request.Email.Trim(), response.Email);
        Assert.Equal(0L, response.TotalXp);
        Assert.Equal(0, response.CurrentStreak);
        Assert.Null(response.Proficiency);
        Assert.Null(response.Bio);
        userRepository.Verify(repository => repository.SaveChangesAsync(cancellationToken), Times.Once);
        tokenService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterAsync_MixedCaseEmail_UsesNormalizedEmailForUniqueness()
    {
        var request = CreateRegisterRequest(email: "  Pedro@Example.com ");
        UserModel? addedUser = null;
        userRepository
            .Setup(repository => repository.ExistsByNormalizedEmailAsync("PEDRO@EXAMPLE.COM", cancellationToken))
            .ReturnsAsync(false);
        userRepository
            .Setup(repository => repository.AddAsync(It.IsAny<UserModel>(), cancellationToken))
            .Callback<UserModel, CancellationToken>((user, _) => addedUser = user)
            .Returns(Task.CompletedTask);
        userRepository.Setup(repository => repository.SaveChangesAsync(cancellationToken)).ReturnsAsync(1);
        passwordHasher
            .Setup(hasher => hasher.HashPassword(It.IsAny<UserModel>(), request.Password))
            .Returns("secure-hash");
        var service = CreateService();

        await service.RegisterAsync(request, cancellationToken);

        Assert.NotNull(addedUser);
        Assert.Equal("PEDRO@EXAMPLE.COM", addedUser.NormalizedEmail);
        Assert.Equal("Pedro@Example.com", addedUser.Email);
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task RegisterAsync_DuplicateNormalizedEmail_ThrowsConflictException()
    {
        var request = CreateRegisterRequest();
        userRepository
            .Setup(repository => repository.ExistsByNormalizedEmailAsync("PEDRO@EXAMPLE.COM", cancellationToken))
            .ReturnsAsync(true);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.RegisterAsync(request, cancellationToken)
        );

        Assert.Equal("Já existe uma conta cadastrada com este e-mail.", exception.Detail);
        userRepository.Verify(
            repository => repository.AddAsync(It.IsAny<UserModel>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RegisterAsync_DatabaseFailure_PropagatesDbUpdateException()
    {
        var request = CreateRegisterRequest();
        userRepository
            .Setup(repository => repository.AddAsync(It.IsAny<UserModel>(), cancellationToken))
            .Returns(Task.CompletedTask);
        userRepository
            .Setup(repository => repository.SaveChangesAsync(cancellationToken))
            .ThrowsAsync(new DbUpdateException());
        passwordHasher
            .Setup(hasher => hasher.HashPassword(It.IsAny<UserModel>(), request.Password))
            .Returns("secure-hash");
        var service = CreateService();

        await Assert.ThrowsAsync<DbUpdateException>(() => service.RegisterAsync(request, cancellationToken));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsBearerTokenAndUser()
    {
        var user = TestData.CreateUser();
        var expiresAt = TestData.Now.AddMinutes(60);
        userRepository
            .Setup(repository => repository.GetByNormalizedEmailAsync(user.NormalizedEmail, cancellationToken))
            .ReturnsAsync(user);
        passwordHasher
            .Setup(hasher => hasher.VerifyHashedPassword(user, user.PasswordHash, "Valid123!"))
            .Returns(PasswordVerificationResult.Success);
        tokenService
            .Setup(service => service.Create(user.Id, user.Email))
            .Returns(new AccessTokenResultDTO { AccessToken = "access-token", ExpiresAt = expiresAt });
        var service = CreateService();

        var response = await service.LoginAsync(
            new LoginUserRequestDTO { Email = user.Email, Password = "Valid123!" },
            cancellationToken
        );

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal(expiresAt, response.ExpiresAt);
        Assert.Equal(user.Id, response.User.Id);
        Assert.Equal(user.Proficiency, response.User.Proficiency);
        Assert.Equal(user.Bio, response.User.Bio);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorizedException()
    {
        userRepository
            .Setup(repository => repository.GetByNormalizedEmailAsync("UNKNOWN@EXAMPLE.COM", cancellationToken))
            .ReturnsAsync((UserModel?)null);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.LoginAsync(
                new LoginUserRequestDTO { Email = "unknown@example.com", Password = "Valid123!" },
                cancellationToken
            )
        );

        Assert.Equal("E-mail ou senha inválidos.", exception.Detail);
        passwordHasher.VerifyNoOtherCalls();
        tokenService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoginAsync_IncorrectPassword_ThrowsSameUnauthorizedExceptionAsUnknownEmail()
    {
        var user = TestData.CreateUser();
        userRepository
            .Setup(repository => repository.GetByNormalizedEmailAsync(user.NormalizedEmail, cancellationToken))
            .ReturnsAsync(user);
        passwordHasher
            .Setup(hasher => hasher.VerifyHashedPassword(user, user.PasswordHash, "Wrong123!"))
            .Returns(PasswordVerificationResult.Failed);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.LoginAsync(
                new LoginUserRequestDTO { Email = user.Email, Password = "Wrong123!" },
                cancellationToken
            )
        );

        Assert.Equal("E-mail ou senha inválidos.", exception.Detail);
        tokenService.VerifyNoOtherCalls();
    }

    private AuthService CreateService()
    {
        return new AuthService(
            userRepository.Object,
            passwordHasher.Object,
            tokenService.Object,
            NullLogger<AuthService>.Instance
        );
    }

    private static CreateUserRequestDTO CreateRegisterRequest(string email = "pedro@example.com")
    {
        return new CreateUserRequestDTO
        {
            FirstName = "Pedro",
            LastName = "Oliveira",
            Email = email,
            Password = "Valid123!",
            PasswordConfirmation = "Valid123!",
        };
    }
}
