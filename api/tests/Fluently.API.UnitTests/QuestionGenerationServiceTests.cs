using Fluently.API.DTOs.Questions;
using Fluently.API.Models;
using Fluently.API.Options;
using Fluently.API.Repositories;
using Fluently.API.Services;

using Microsoft.Extensions.Options;

using Moq;

namespace Fluently.API.UnitTests;

public sealed class QuestionGenerationServiceTests
{
    private readonly Mock<ILanguageModelClient> languageModelClient = new();
    private readonly Mock<IQuestionRepository> questionRepository = new();
    private readonly CancellationToken cancellationToken = new CancellationTokenSource().Token;

    [Fact]
    public async Task GenerateAsync_ValidOutput_ReturnsQuestionWithTranslationsAndCorrectIndex()
    {
        var user = TestData.CreateUser();
        languageModelClient.Setup(client => client.GetStructuredResponseAsync<QuestionGenerationOutputDTO>(
                It.IsAny<string>(), It.IsAny<string>(), 1, cancellationToken))
            .ReturnsAsync(CreateOutput());
        questionRepository.Setup(repository => repository.ExistsContextFingerprintAsync(
                user.Id, It.IsAny<string>(), cancellationToken))
            .ReturnsAsync(false);

        var result = await CreateService().GenerateAsync(user, cancellationToken);

        Assert.Equal("Eu estudo inglês todos os dias.", result.QuestionTranslation);
        Assert.Equal(5, result.Alternatives.Count);
        Assert.InRange(result.CorrectAlternativeIndex, 1, 5);
        Assert.Equal("estudo", result.Alternatives[result.CorrectAlternativeIndex - 1].Translation);
    }

    [Fact]
    public async Task GenerateAsync_UsesOnlyCurrentProfileInPrompt()
    {
        var user = TestData.CreateUser();
        var incorrectQuestion = TestData.CreateQuestion(user);
        incorrectQuestion.IsCorrect = false;
        incorrectQuestion.AnsweredAt = TestData.Now;
        incorrectQuestion.SubmittedAlternativeIndex = 2;
        string? prompt = null;
        languageModelClient.Setup(client => client.GetStructuredResponseAsync<QuestionGenerationOutputDTO>(
                It.IsAny<string>(), It.IsAny<string>(), 1, cancellationToken))
            .Callback<string, string, float, CancellationToken>((_, userPrompt, _, _) => prompt = userPrompt)
            .ReturnsAsync(CreateOutput());
        questionRepository.Setup(repository => repository.ExistsContextFingerprintAsync(
                user.Id, It.IsAny<string>(), cancellationToken))
            .ReturnsAsync(false);

        await CreateService().GenerateAsync(user, cancellationToken);

        Assert.NotNull(prompt);
        Assert.Contains("Biography: " + user.Bio, prompt);
        Assert.DoesNotContain(incorrectQuestion.Question, prompt);
        questionRepository.Verify(
            repository => repository.GetRecentContextsAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        questionRepository.Verify(
            repository => repository.GetRecentIncorrectAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private QuestionGenerationService CreateService()
    {
        return new QuestionGenerationService(languageModelClient.Object, questionRepository.Object,
            Microsoft.Extensions.Options.Options.Create(new OpenAIOptions { GenerationTemperature = 1 }));
    }

    private static QuestionGenerationOutputDTO CreateOutput()
    {
        return new QuestionGenerationOutputDTO
        {
            Context = "Pedro está descrevendo sua rotina de estudos.",
            Question = "I ___ English every day.",
            QuestionTranslation = "Eu estudo inglês todos os dias.",
            Alternatives =
            [
                new QuestionAlternativeOutputDTO { Text = "study", Translation = "estudo" },
                new QuestionAlternativeOutputDTO { Text = "practice", Translation = "praticar" },
                new QuestionAlternativeOutputDTO { Text = "speak", Translation = "falar" },
                new QuestionAlternativeOutputDTO { Text = "read", Translation = "ler" },
                new QuestionAlternativeOutputDTO { Text = "write", Translation = "escrever" }
            ],
            CorrectAlternativeIndex = 1
        };
    }
}
