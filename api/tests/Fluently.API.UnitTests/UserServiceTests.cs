using Fluently.API.DTOs.Users;
using Fluently.API.Enums;
using Fluently.API.Exceptions;
using Fluently.API.Models;
using Fluently.API.Repositories;
using Fluently.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fluently.API.UnitTests;

public sealed class UserServiceTests
{
    private readonly Mock<ICurrentUserService> currentUserService = new();
    private readonly Mock<IUserRepository> userRepository = new();
    private readonly Mock<IQuestionRepository> questionRepository = new();
    private readonly Mock<IPasswordHasher<UserModel>> passwordHasher = new();
    private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;

    [Fact]
    public async Task GetCurrentAsync_ExistingUser_ReturnsUserProfileAndProgress()
    {
        var user = TestData.CreateUser();
        user.TotalXp = 45;
        user.CurrentStreak = 2;
        SetupCurrentUser(user);
        var service = CreateService();

        var response = await service.GetCurrentAsync(cancellationToken);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(45L, response.TotalXp);
        Assert.Equal(2, response.CurrentStreak);
        Assert.Equal(user.Proficiency, response.Proficiency);
        Assert.Equal(user.Bio, response.Bio);
    }

    [Fact]
    public async Task GetCurrentAsync_MissingUser_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        currentUserService.Setup(service => service.GetUserId()).Returns(userId);
        userRepository
            .Setup(repository => repository.GetByIdAsync(userId, cancellationToken))
            .ReturnsAsync((UserModel?)null);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.GetCurrentAsync(cancellationToken));

        Assert.Equal("O usuário autenticado não foi encontrado.", exception.Detail);
    }

    [Fact]
    public async Task UpdateProfileAsync_CompleteProfile_PersistsProfile()
    {
        var user = TestData.CreateUser();
        var originalEmail = user.Email;
        var request = new UpdateUserRequestDTO
        {
            FirstName = " Ana ",
            LastName = " Silva ",
            Proficiency = ProficiencyLevelEnum.B2,
            Bio = "  Quero viajar sozinho.  ",
        };
        SetupCurrentUser(user);
        userRepository.Setup(repository => repository.SaveChangesAsync(cancellationToken)).ReturnsAsync(1);
        var service = CreateService();

        var response = await service.UpdateProfileAsync(request, cancellationToken);

        Assert.Equal(originalEmail, user.Email);
        Assert.Equal("Ana", user.FirstName);
        Assert.Equal("Silva", user.LastName);
        Assert.Equal(ProficiencyLevelEnum.B2, user.Proficiency);
        Assert.Equal("Quero viajar sozinho.", user.Bio);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(user.Proficiency, response.Proficiency);
        Assert.Equal(user.Bio, response.Bio);
        userRepository.Verify(repository => repository.Update(user), Times.Once);
        userRepository.Verify(repository => repository.SaveChangesAsync(cancellationToken), Times.Once);
        questionRepository.Verify(repository => repository.DeleteCurrentAsync(user.Id, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateCredentialsAsync_DuplicateEmail_ThrowsConflictException()
    {
        var user = TestData.CreateUser();
        var request = new UpdateUserCredentialsRequestDTO
        {
            Email = "used@example.com",
            Password = "Updated123!",
            PasswordConfirmation = "Updated123!",
        };
        SetupCurrentUser(user);
        userRepository
            .Setup(repository => repository.ExistsByNormalizedEmailAsync("USED@EXAMPLE.COM", cancellationToken))
            .ReturnsAsync(true);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateCredentialsAsync(request, cancellationToken)
        );

        Assert.Equal("Já existe uma conta cadastrada com este e-mail.", exception.Detail);
        passwordHasher.VerifyNoOtherCalls();
        userRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCredentialsAsync_ValidCredentials_UpdatesEmailAndPassword()
    {
        var user = TestData.CreateUser();
        var request = new UpdateUserCredentialsRequestDTO
        {
            Email = "  updated@example.com ",
            Password = "Updated123!",
            PasswordConfirmation = "Updated123!",
        };
        SetupCurrentUser(user);
        userRepository
            .Setup(repository => repository.ExistsByNormalizedEmailAsync("UPDATED@EXAMPLE.COM", cancellationToken))
            .ReturnsAsync(false);
        passwordHasher.Setup(hasher => hasher.HashPassword(user, request.Password)).Returns("updated-hash");
        userRepository.Setup(repository => repository.SaveChangesAsync(cancellationToken)).ReturnsAsync(1);
        var service = CreateService();

        await service.UpdateCredentialsAsync(request, cancellationToken);

        Assert.Equal("updated@example.com", user.Email);
        Assert.Equal("UPDATED@EXAMPLE.COM", user.NormalizedEmail);
        Assert.Equal("updated-hash", user.PasswordHash);
        userRepository.Verify(repository => repository.Update(user), Times.Once);
        userRepository.Verify(repository => repository.SaveChangesAsync(cancellationToken), Times.Once);
    }

    private UserService CreateService()
    {
        return new UserService(
            currentUserService.Object,
            userRepository.Object,
            questionRepository.Object,
            passwordHasher.Object,
            NullLogger<UserService>.Instance,
            new FixedTimeProvider(TestData.Now)
        );
    }

    private void SetupCurrentUser(UserModel user)
    {
        currentUserService.Setup(service => service.GetUserId()).Returns(user.Id);
        userRepository.Setup(repository => repository.GetByIdAsync(user.Id, cancellationToken)).ReturnsAsync(user);
    }
}
