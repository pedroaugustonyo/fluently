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
        SetupHistory(user);
        languageModelClient.Setup(client => client.GetStructuredResponseAsync<QuestionGenerationOutputDTO>(
                It.IsAny<string>(), It.IsAny<string>(), 1, cancellationToken))
            .ReturnsAsync(CreateOutput());
        questionRepository.Setup(repository => repository.ExistsContextFingerprintAsync(
                user.Id, It.IsAny<string>(), cancellationToken))
            .ReturnsAsync(false);

        var result = await CreateService().GenerateAsync(user, cancellationToken);

        Assert.Equal("Eu estudo inglês todos os dias.", result.QuestionTranslation);
        Assert.Equal(5, result.Alternatives.Count);
        Assert.Equal(1, result.CorrectAlternativeIndex);
        Assert.Equal("estudo", result.Alternatives[0].Translation);
    }

    [Fact]
    public async Task GenerateAsync_PreviousIncorrectQuestions_IncludesThemInPrompt()
    {
        var user = TestData.CreateUser();
        var incorrectQuestion = TestData.CreateQuestion(user);
        incorrectQuestion.IsCorrect = false;
        incorrectQuestion.AnsweredAt = TestData.Now;
        incorrectQuestion.SubmittedAlternativeIndex = 2;
        SetupHistory(user, [incorrectQuestion]);
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
        Assert.Contains(incorrectQuestion.Question, prompt);
        Assert.Contains("Correct alternative: study", prompt);
    }

    private QuestionGenerationService CreateService()
    {
        return new QuestionGenerationService(languageModelClient.Object, questionRepository.Object,
            Microsoft.Extensions.Options.Options.Create(new OpenAIOptions { GenerationTemperature = 1 }));
    }

    private void SetupHistory(UserModel user, IReadOnlyList<QuestionModel>? incorrectQuestions = null)
    {
        questionRepository.Setup(repository => repository.GetRecentContextsAsync(user.Id, 20, cancellationToken))
            .ReturnsAsync([]);
        questionRepository.Setup(repository => repository.GetRecentIncorrectAsync(user.Id, 10, cancellationToken))
            .ReturnsAsync(incorrectQuestions ?? []);
    }

    private static QuestionGenerationOutputDTO CreateOutput()
    {
        return new QuestionGenerationOutputDTO
        {
            Context = "Pedro está descrevendo sua rotina de estudos.",
            Question = "I ? English every day.",
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
