using Fluently.API.Controllers.v1;
using Fluently.API.DTOs.Common;
using Fluently.API.DTOs.Leaderboard;
using Fluently.API.DTOs.Questions;
using Fluently.API.DTOs.Users;
using Fluently.API.Enums;
using Fluently.API.Services;

using Microsoft.AspNetCore.Mvc;

using Moq;

namespace Fluently.API.UnitTests;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsCreatedWithCurrentUserLocation()
    {
        var service = new Mock<IAuthService>();
        var request = new CreateUserRequestDTO
        {
            FirstName = "Pedro",
            LastName = "Oliveira",
            Email = "pedro@example.com",
            Password = "Valid123!",
            PasswordConfirmation = "Valid123!"
        };
        var response = CreateCreateUserResponse();
        using var cancellationSource = new CancellationTokenSource();
        service
            .Setup(candidate => candidate.RegisterAsync(request, cancellationSource.Token))
            .ReturnsAsync(response);
        var controller = new AuthController(service.Object);

        var action = await controller.RegisterAsync(request, cancellationSource.Token);
        var result = Assert.IsType<CreatedResult>(action.Result);

        Assert.Equal("/api/v1/users/me", result.Location);
        Assert.Same(response, result.Value);
    }

    [Fact]
    public async Task LoginAsync_ValidRequest_ReturnsOkAndForwardsCancellation()
    {
        var service = new Mock<IAuthService>();
        var request = new LoginUserRequestDTO
        {
            Email = "pedro@example.com",
            Password = "Valid123!"
        };
        var response = new LoginUserResponseDTO
        {
            AccessToken = "token",
            TokenType = "Bearer",
            ExpiresAt = TestData.Now.AddHours(1),
            User = CreateLoginUserDetailsResponse()
        };
        using var cancellationSource = new CancellationTokenSource();
        service
            .Setup(candidate => candidate.LoginAsync(request, cancellationSource.Token))
            .ReturnsAsync(response);
        var controller = new AuthController(service.Object);

        var action = await controller.LoginAsync(request, cancellationSource.Token);
        var result = Assert.IsType<OkObjectResult>(action.Result);

        Assert.Same(response, result.Value);
        service.VerifyAll();
    }

    private static CreateUserResponseDTO CreateCreateUserResponse()
    {
        return new CreateUserResponseDTO
        {
            Id = Guid.NewGuid(),
            FirstName = "Pedro",
            LastName = "Oliveira",
            Email = "pedro@example.com",
            TotalXp = 0,
            CurrentStreak = 0,
            Proficiency = ProficiencyLevelEnum.A1,
            CreatedAt = TestData.Now
        };
    }

    private static LoginUserDetailsResponseDTO CreateLoginUserDetailsResponse()
    {
        return new LoginUserDetailsResponseDTO
        {
            Id = Guid.NewGuid(),
            FirstName = "Pedro",
            LastName = "Oliveira",
            Email = "pedro@example.com",
            Proficiency = ProficiencyLevelEnum.A1,
            CreatedAt = TestData.Now
        };
    }
}

public sealed class UsersControllerTests
{
    private readonly Guid userId = Guid.NewGuid();

    [Fact]
    public async Task GetCurrentAsync_ServiceResponse_ReturnsOkWithoutReadingHttpContext()
    {
        var service = new Mock<IUserService>();
        var response = CreateGetUserResponse();
        using var cancellationSource = new CancellationTokenSource();
        service
            .Setup(candidate => candidate.GetCurrentAsync(cancellationSource.Token))
            .ReturnsAsync(response);
        var controller = new UsersController(service.Object);

        var action = await controller.GetCurrentAsync(cancellationSource.Token);
        var result = Assert.IsType<OkObjectResult>(action.Result);

        Assert.Same(response, result.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task UpdateProfileAsync_ValidRequest_ReturnsOkAndForwardsCancellation()
    {
        var service = new Mock<IUserService>();
        var request = new UpdateUserRequestDTO
        {
            FirstName = "Pedro",
            LastName = "Oliveira",
            Proficiency = ProficiencyLevelEnum.A2,
            Bio = "Quero praticar para uma viagem."
        };
        var response = CreateUpdateUserResponse();
        using var cancellationSource = new CancellationTokenSource();
        service
            .Setup(candidate => candidate.UpdateProfileAsync(request, cancellationSource.Token))
            .ReturnsAsync(response);
        var controller = new UsersController(service.Object);

        var action = await controller.UpdateProfileAsync(request, cancellationSource.Token);
        var result = Assert.IsType<OkObjectResult>(action.Result);

        Assert.Same(response, result.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task UpdateCredentialsAsync_ValidRequest_ReturnsNoContent()
    {
        var service = new Mock<IUserService>();
        var request = new UpdateUserCredentialsRequestDTO
        {
            Email = "updated@example.com",
            Password = "Updated123!",
            PasswordConfirmation = "Updated123!"
        };
        using var cancellationSource = new CancellationTokenSource();
        service
            .Setup(candidate => candidate.UpdateCredentialsAsync(request, cancellationSource.Token))
            .Returns(Task.CompletedTask);
        var controller = new UsersController(service.Object);

        var result = await controller.UpdateCredentialsAsync(request, cancellationSource.Token);

        Assert.IsType<NoContentResult>(result);
        service.VerifyAll();
    }

    private GetUserResponseDTO CreateGetUserResponse()
    {
        return new GetUserResponseDTO
        {
            Id = userId,
            FirstName = "Pedro",
            LastName = "Oliveira",
            Email = "pedro@example.com",
            TotalXp = 0,
            CurrentStreak = 0,
            Proficiency = ProficiencyLevelEnum.A2,
            Bio = "Quero praticar para uma viagem.",
            CreatedAt = TestData.Now
        };
    }

    private UpdateUserResponseDTO CreateUpdateUserResponse()
    {
        return new UpdateUserResponseDTO
        {
            Id = userId,
            FirstName = "Pedro",
            LastName = "Oliveira",
            Email = "pedro@example.com",
            Proficiency = ProficiencyLevelEnum.A2,
            Bio = "Quero praticar para uma viagem.",
            CreatedAt = TestData.Now
        };
    }
}

public sealed class QuestionsControllerTests
{
    [Fact]
    public async Task GetCurrentAsync_ServiceResponseWithCompleteQuestion_ReturnsOk()
    {
        var service = new Mock<IQuestionService>();
        var response = CreateQuestionResponse();
        using var cancellationSource = new CancellationTokenSource();
        service
            .Setup(candidate => candidate.GetCurrentAsync(cancellationSource.Token))
            .ReturnsAsync(response);
        var controller = new QuestionsController(service.Object);

        var action = await controller.GetCurrentAsync(cancellationSource.Token);
        var result = Assert.IsType<OkObjectResult>(action.Result);

        Assert.Same(response, result.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task CreateAsync_ServiceResponseWithCompleteQuestion_ReturnsCreatedWithQuestionLocation()
    {
        var service = new Mock<IQuestionService>();
        var response = CreateQuestionResponse();
        using var cancellationSource = new CancellationTokenSource();
        service
            .Setup(candidate => candidate.CreateAsync(cancellationSource.Token))
            .ReturnsAsync(response);
        var controller = new QuestionsController(service.Object);

        var action = await controller.CreateAsync(cancellationSource.Token);
        var result = Assert.IsType<CreatedResult>(action.Result);

        Assert.Equal($"/api/v1/questions/{response.Id}", result.Location);
        Assert.Same(response, result.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task GetAllAsync_ServiceResponseWithCompleteQuestions_ReturnsOk()
    {
        var service = new Mock<IQuestionService>();
        var request = new PaginationRequestDTO();
        var response = new PaginatedResponseDTO<QuestionDetailsResponseDTO>
        {
            Items = [CreateQuestionDetailsResponse()],
            Page = 1,
            PageSize = 20,
            TotalItems = 1,
            TotalPages = 1
        };
        using var cancellationSource = new CancellationTokenSource();
        service
            .Setup(candidate => candidate.GetAllAsync(request, cancellationSource.Token))
            .ReturnsAsync(response);
        var controller = new QuestionsController(service.Object);

        var action = await controller.GetAllAsync(request, cancellationSource.Token);
        var result = Assert.IsType<OkObjectResult>(action.Result);

        Assert.Same(response, result.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task GetByIdAsync_ServiceResponseWithCompleteQuestion_ReturnsOk()
    {
        var service = new Mock<IQuestionService>();
        var response = CreateQuestionDetailsResponse();
        using var cancellationSource = new CancellationTokenSource();
        service
            .Setup(candidate => candidate.GetByIdAsync(response.Id, cancellationSource.Token))
            .ReturnsAsync(response);
        var controller = new QuestionsController(service.Object);

        var action = await controller.GetByIdAsync(response.Id, cancellationSource.Token);
        var result = Assert.IsType<OkObjectResult>(action.Result);

        Assert.Same(response, result.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task SubmitAnswerAsync_FirstAnswer_ReturnsCreatedWithQuestionLocation()
    {
        var service = new Mock<IQuestionService>();
        var questionId = Guid.NewGuid();
        var request = new SubmitQuestionAnswerRequestDTO { AlternativeIndex = 1 };
        var response = new QuestionAnswerResponseDTO
        {
            QuestionId = questionId,
            IsCorrect = true,
            AwardedXp = 15,
            TotalXp = 15,
            CurrentStreak = 1,
            CorrectAlternative = new QuestionAlternativeResponseDTO
            {
                Index = 1,
                Text = "study",
                Translation = "estudar"
            },
            QuestionTranslation = "Eu estudo inglês todos os dias.",
            AnsweredAt = TestData.Now
        };
        using var cancellationSource = new CancellationTokenSource();
        service
            .Setup(candidate => candidate.SubmitAnswerAsync(
                questionId,
                request,
                cancellationSource.Token))
            .ReturnsAsync(response);
        var controller = new QuestionsController(service.Object);

        var action = await controller.SubmitAnswerAsync(
            questionId,
            request,
            cancellationSource.Token);
        var result = Assert.IsType<CreatedResult>(action.Result);

        Assert.Equal($"/api/v1/questions/{questionId}", result.Location);
        Assert.Same(response, result.Value);
        service.VerifyAll();
    }

    private static QuestionResponseDTO CreateQuestionResponse()
    {
        return new QuestionResponseDTO
        {
            Id = Guid.NewGuid(),
            Context = "Pedro está praticando inglês antes de uma viagem.",
            Question = "I ? English every day.",
            Alternatives = CreateAlternatives(),
            BaseXp = 15,
            CreatedAt = TestData.Now
        };
    }

    private static QuestionDetailsResponseDTO CreateQuestionDetailsResponse()
    {
        return new QuestionDetailsResponseDTO
        {
            Id = Guid.NewGuid(),
            Context = "Pedro está praticando inglês antes de uma viagem.",
            Question = "I ? English every day.",
            Alternatives = CreateAlternatives(),
            QuestionTranslation = "Eu estudo inglês todos os dias.",
            CorrectAlternative = CreateAlternatives()[0],
            BaseXp = 15,
            SubmittedAlternativeIndex = 1,
            IsCorrect = true,
            AwardedXp = 15,
            CreatedAt = TestData.Now,
            AnsweredAt = TestData.Now
        };
    }

    private static IReadOnlyList<QuestionAlternativeResponseDTO> CreateAlternatives()
    {
        return
        [
            new QuestionAlternativeResponseDTO { Index = 1, Text = "study", Translation = "estudar" },
            new QuestionAlternativeResponseDTO { Index = 2, Text = "practice", Translation = "praticar" },
            new QuestionAlternativeResponseDTO { Index = 3, Text = "speak", Translation = "falar" },
            new QuestionAlternativeResponseDTO { Index = 4, Text = "read", Translation = "ler" },
            new QuestionAlternativeResponseDTO { Index = 5, Text = "write", Translation = "escrever" }
        ];
    }
}

public sealed class LeaderboardControllerTests
{
    [Fact]
    public async Task GetAsync_ValidPagination_ReturnsOkAndForwardsCancellation()
    {
        var service = new Mock<ILeaderboardService>();
        var request = new PaginationRequestDTO { Page = 2, PageSize = 10 };
        var response = new PaginatedResponseDTO<LeaderboardEntryResponseDTO>
        {
            Items = [],
            Page = 2,
            PageSize = 10,
            TotalItems = 0,
            TotalPages = 0
        };
        using var cancellationSource = new CancellationTokenSource();
        service
            .Setup(candidate => candidate.GetAsync(request, cancellationSource.Token))
            .ReturnsAsync(response);
        var controller = new LeaderboardController(service.Object);

        var action = await controller.GetAsync(request, cancellationSource.Token);
        var result = Assert.IsType<OkObjectResult>(action.Result);

        Assert.Same(response, result.Value);
        service.VerifyAll();
    }
}
