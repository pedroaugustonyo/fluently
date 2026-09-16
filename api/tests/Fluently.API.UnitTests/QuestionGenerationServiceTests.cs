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
        questionRepository.Setup(repository => repository.CountByUserAsync(user.Id, cancellationToken))
            .ReturnsAsync(0);

        var result = await CreateService().GenerateAsync(user, cancellationToken);

        Assert.Equal("Eu estudo inglês todos os dias.", result.QuestionTranslation);
        Assert.Equal(5, result.Alternatives.Count);
        Assert.InRange(result.CorrectAlternativeIndex, 1, 5);
        Assert.Equal("estudo", result.Alternatives[result.CorrectAlternativeIndex - 1].Translation);
    }

    [Fact]
    public async Task GenerateAsync_IncludesCurrentProfileInPrompt()
    {
        var user = TestData.CreateUser();
        string? systemPrompt = null;
        string? prompt = null;
        languageModelClient.Setup(client => client.GetStructuredResponseAsync<QuestionGenerationOutputDTO>(
                It.IsAny<string>(), It.IsAny<string>(), 1, cancellationToken))
            .Callback<string, string, float, CancellationToken>((promptInstructions, userPrompt, _, _) =>
            {
                systemPrompt = promptInstructions;
                prompt = userPrompt;
            })
            .ReturnsAsync(CreateOutput());
        questionRepository.Setup(repository => repository.ExistsContextFingerprintAsync(
                user.Id, It.IsAny<string>(), cancellationToken))
            .ReturnsAsync(false);
        questionRepository.Setup(repository => repository.CountByUserAsync(user.Id, cancellationToken))
            .ReturnsAsync(2);

        await CreateService().GenerateAsync(user, cancellationToken);

        Assert.NotNull(prompt);
        Assert.Contains("Biography: " + user.Bio, prompt);
        Assert.Contains("Question sequence number: 3", prompt);
        Assert.NotNull(systemPrompt);
        Assert.Contains("rotate through every interest", systemPrompt);
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
