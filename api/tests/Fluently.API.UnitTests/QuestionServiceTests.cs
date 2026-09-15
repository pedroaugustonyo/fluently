using Fluently.API.DTOs.Common;
using Fluently.API.DTOs.Questions;
using Fluently.API.Exceptions;
using Fluently.API.Models;
using Fluently.API.Repositories;
using Fluently.API.Services;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace Fluently.API.UnitTests;

public sealed class QuestionServiceTests
{
    private readonly Mock<ICurrentUserService> currentUserService = new();
    private readonly Mock<IUserRepository> userRepository = new();
    private readonly Mock<IQuestionRepository> questionRepository = new();
    private readonly Mock<IQuestionGenerationService> generationService = new();
    private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;

    [Fact]
    public async Task GetCurrentAsync_PendingQuestion_ReturnsIndexedAlternativesWithoutAnswer()
    {
        var question = TestData.CreateQuestion();
        SetCurrentUser(question.UserId);
        questionRepository.Setup(repository => repository.GetCurrentAsync(question.UserId, cancellationToken))
            .ReturnsAsync(question);

        var response = await CreateService().GetCurrentAsync(cancellationToken);

        Assert.Equal(question.Id, response.Id);
        Assert.Equal(5, response.Alternatives.Count);
        Assert.Equal(1, response.Alternatives[0].Index);
        Assert.Equal("study", response.Alternatives[0].Text);
    }

    [Fact]
    public async Task GetAllAsync_PendingAndAnsweredQuestions_OnlyExposesAnswerForAnsweredQuestion()
    {
        var pendingQuestion = TestData.CreateQuestion();
        var answeredQuestion = CreateAnsweredQuestion(false);
        answeredQuestion.Id = Guid.NewGuid();
        SetCurrentUser(pendingQuestion.UserId);
        questionRepository.Setup(repository => repository.CountByUserAsync(pendingQuestion.UserId, cancellationToken))
            .ReturnsAsync(2);
        questionRepository.Setup(repository => repository.GetPageByUserAsync(pendingQuestion.UserId, 0, 20, cancellationToken))
            .ReturnsAsync([pendingQuestion, answeredQuestion]);

        var response = await CreateService().GetAllAsync(new PaginationRequestDTO(), cancellationToken);

        Assert.Equal(2, response.TotalItems);
        Assert.Null(response.Items[0].CorrectAlternative);
        Assert.Null(response.Items[0].QuestionTranslation);
        Assert.NotNull(response.Items[1].CorrectAlternative);
        Assert.Equal(answeredQuestion.QuestionTranslation, response.Items[1].QuestionTranslation);
    }

    [Fact]
    public async Task CreateAsync_IncompleteLearningSettings_ThrowsBadRequestException()
    {
        var user = TestData.CreateUser();
        user.Proficiency = null;
        user.Bio = " ";
        SetCurrentUser(user.Id);
        userRepository.Setup(repository => repository.GetByIdAsync(user.Id, cancellationToken)).ReturnsAsync(user);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(cancellationToken));

        Assert.Equal("Preencha seu nível de proficiência e biografia antes de gerar uma questão.", exception.Detail);
    }

    [Fact]
    public async Task CreateAsync_PendingQuestionExists_ThrowsConflictException()
    {
        var user = TestData.CreateUser();
        SetCurrentUser(user.Id);
        userRepository.Setup(repository => repository.GetByIdAsync(user.Id, cancellationToken)).ReturnsAsync(user);
        questionRepository.Setup(repository => repository.GetCurrentAsync(user.Id, cancellationToken))
            .ReturnsAsync(TestData.CreateQuestion(user));

        var exception = await Assert.ThrowsAsync<ConflictException>(() => CreateService().CreateAsync(cancellationToken));

        Assert.Equal("Responda a questão atual antes de solicitar uma nova questão.", exception.Detail);
    }

    [Fact]
    public async Task CreateAsync_ValidGeneration_PersistsIndexedAlternativesWithoutExposingAnswer()
    {
        var user = TestData.CreateUser();
        QuestionModel? savedQuestion = null;
        SetCurrentUser(user.Id);
        userRepository.Setup(repository => repository.GetByIdAsync(user.Id, cancellationToken)).ReturnsAsync(user);
        questionRepository.Setup(repository => repository.GetCurrentAsync(user.Id, cancellationToken))
            .ReturnsAsync((QuestionModel?)null);
        generationService.Setup(service => service.GenerateAsync(user, cancellationToken))
            .ReturnsAsync(CreateGeneratedQuestion());
        questionRepository.Setup(repository => repository.AddAsync(It.IsAny<QuestionModel>(), cancellationToken))
            .Callback<QuestionModel, CancellationToken>((question, _) => savedQuestion = question)
            .Returns(Task.CompletedTask);
        questionRepository.Setup(repository => repository.SaveChangesAsync(cancellationToken)).ReturnsAsync(1);

        var response = await CreateService().CreateAsync(cancellationToken);

        Assert.NotNull(savedQuestion);
        Assert.Equal(1, savedQuestion.CorrectAlternativeIndex);
        Assert.Equal("Eu estudo inglês todos os dias.", savedQuestion.QuestionTranslation);
        Assert.Equal(5, response.Alternatives.Count);
        Assert.Equal(5, savedQuestion.AlternativeTranslations.Length);
    }

    [Fact]
    public async Task SubmitAnswerAsync_CorrectAlternative_UpdatesProgressAndReturnsSentenceTranslation()
    {
        var user = TestData.CreateUser();
        var question = TestData.CreateQuestion(user);
        SetupOwnedQuestion(question);
        questionRepository.Setup(repository => repository.SaveChangesAsync(cancellationToken)).ReturnsAsync(1);

        var response = await CreateService().SubmitAnswerAsync(
            question.Id,
            new SubmitQuestionAnswerRequestDTO { AlternativeIndex = 1 },
            cancellationToken);

        Assert.True(response.IsCorrect);
        Assert.Equal(15, response.AwardedXp);
        Assert.Equal(1, response.CorrectAlternative.Index);
        Assert.Equal(question.QuestionTranslation, response.QuestionTranslation);
        Assert.Equal(1, question.SubmittedAlternativeIndex);
    }

    [Fact]
    public async Task SubmitAnswerAsync_IncorrectAlternative_ResetsStreakAndExposesCorrectAlternative()
    {
        var user = TestData.CreateUser();
        user.CurrentStreak = 3;
        user.TotalXp = 45;
        var question = TestData.CreateQuestion(user);
        SetupOwnedQuestion(question);
        questionRepository.Setup(repository => repository.SaveChangesAsync(cancellationToken)).ReturnsAsync(1);

        var response = await CreateService().SubmitAnswerAsync(
            question.Id,
            new SubmitQuestionAnswerRequestDTO { AlternativeIndex = 2 },
            cancellationToken);

        Assert.False(response.IsCorrect);
        Assert.Equal(0, response.AwardedXp);
        Assert.Equal(0, response.CurrentStreak);
        Assert.Equal(1, response.CorrectAlternative.Index);
        Assert.Equal(2, question.SubmittedAlternativeIndex);
    }

    private QuestionService CreateService()
    {
        return new QuestionService(currentUserService.Object, userRepository.Object, questionRepository.Object,
            generationService.Object, NullLogger<QuestionService>.Instance);
    }

    private void SetCurrentUser(Guid userId)
    {
        currentUserService.Setup(service => service.GetUserId()).Returns(userId);
    }

    private void SetupOwnedQuestion(QuestionModel question)
    {
        SetCurrentUser(question.UserId);
        questionRepository.Setup(repository => repository.GetOwnedAsync(question.Id, question.UserId, cancellationToken))
            .ReturnsAsync(question);
    }

    private static GeneratedQuestionDTO CreateGeneratedQuestion()
    {
        return new GeneratedQuestionDTO
        {
            Context = "Pedro está descrevendo sua rotina de estudos.",
            Question = "I ? English every day.",
            QuestionTranslation = "Eu estudo inglês todos os dias.",
            Alternatives =
            [
                new QuestionAlternativeOutputDTO { Text = "study", Translation = "estudar" },
                new QuestionAlternativeOutputDTO { Text = "practice", Translation = "praticar" },
                new QuestionAlternativeOutputDTO { Text = "speak", Translation = "falar" },
                new QuestionAlternativeOutputDTO { Text = "read", Translation = "ler" },
                new QuestionAlternativeOutputDTO { Text = "write", Translation = "escrever" }
            ],
            CorrectAlternativeIndex = 1,
            ContextFingerprint = "HASH"
        };
    }

    private static QuestionModel CreateAnsweredQuestion(bool isCorrect)
    {
        var question = TestData.CreateQuestion();
        question.SubmittedAlternativeIndex = isCorrect ? 1 : 2;
        question.IsCorrect = isCorrect;
        question.AwardedXp = isCorrect ? 15 : 0;
        question.StreakAfterAnswer = isCorrect ? 1 : 0;
        question.AnsweredAt = TestData.Now;

        return question;
    }
}
