namespace Fluently.API.Services;

/// <summary>
/// Operações de consulta a um modelo de linguagem.
/// </summary>
public interface ILanguageModelClient
{
    /// <summary>
    /// Solicita uma resposta estruturada ao modelo de linguagem.
    /// </summary>
    /// <param name="systemPrompt">Instruções de sistema enviadas ao modelo.</param>
    /// <param name="userPrompt">Conteúdo da solicitação do usuário.</param>
    /// <param name="temperature">Temperatura aplicada à geração.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <typeparam name="T">Tipo esperado para a resposta estruturada.</typeparam>
    /// <returns>Resposta desserializada para o tipo solicitado.</returns>
    Task<T> GetStructuredResponseAsync<T>(string systemPrompt,
                                          string userPrompt,
                                          float temperature,
                                          CancellationToken cancellationToken)
        where T : class;
}
