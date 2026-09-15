using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Fluently.API.Options;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Fluently.API.Services;

/// <summary>
/// Consultas ao modelo de linguagem e respostas estruturadas.
/// </summary>
public sealed class LanguageModelClient : ILanguageModelClient
{
    /// <summary>
    /// Opções de serialização das respostas estruturadas.
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    /// <summary>
    /// Cliente de conversação.
    /// </summary>
    private readonly IChatClient _chatClient;

    /// <summary>
    /// Configurações da OpenAI.
    /// </summary>
    private readonly IOptions<OpenAIOptions> _options;

    /// <summary>
    /// Inicializa uma nova instância do cliente do modelo de linguagem.
    /// </summary>
    /// <param name="chatClient">Cliente utilizado para enviar mensagens ao modelo.</param>
    /// <param name="options">Configurações utilizadas nas consultas ao modelo.</param>
    public LanguageModelClient(IChatClient chatClient, IOptions<OpenAIOptions> options)
    {
        _chatClient = chatClient;
        _options = options;
    }

    /// <summary>
    /// Solicita uma resposta estruturada ao modelo de linguagem.
    /// </summary>
    /// <param name="systemPrompt">Instruções de sistema enviadas ao modelo.</param>
    /// <param name="userPrompt">Conteúdo da solicitação do usuário.</param>
    /// <param name="temperature">Temperatura aplicada à geração.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <typeparam name="T">Tipo esperado para a resposta estruturada.</typeparam>
    /// <returns>Resposta desserializada para o tipo solicitado.</returns>
    public async Task<T> GetStructuredResponseAsync<T>(string systemPrompt,
                                                       string userPrompt,
                                                       float temperature,
                                                       CancellationToken cancellationToken)
        where T : class
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(_options.Value.TimeoutSeconds));

        var response = await _chatClient.GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, systemPrompt),
                new ChatMessage(ChatRole.User, userPrompt)
            ],
            new ChatOptions
            {
                Temperature = temperature,
                MaxOutputTokens = _options.Value.MaxOutputTokens,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<T>(SerializerOptions)
            },
            timeoutSource.Token);
        var json = ExtractJson(response.Text);

        return JsonSerializer.Deserialize<T>(json, SerializerOptions)
            ?? throw new InvalidOperationException("The language model returned an empty result.");
    }

    /// <summary>
    /// Extrai o conteúdo JSON de uma resposta que pode conter delimitadores Markdown.
    /// </summary>
    /// <param name="response">Resposta textual retornada pelo modelo.</param>
    /// <returns>Conteúdo JSON sem delimitadores externos.</returns>
    private static string ExtractJson(string response)
    {
        var value = response.Trim();

        if (!value.StartsWith("```", StringComparison.Ordinal))
        {
            return value;
        }

        var firstLineBreak = value.IndexOf('\n');
        var lastFence = value.LastIndexOf("```", StringComparison.Ordinal);

        return firstLineBreak >= 0 && lastFence > firstLineBreak
            ? value[(firstLineBreak + 1)..lastFence].Trim()
            : value;
    }
}
