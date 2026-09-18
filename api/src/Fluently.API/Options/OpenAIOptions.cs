using System.ComponentModel.DataAnnotations;

namespace Fluently.API.Options;

/// <summary>
/// Configurações da integração com a OpenAI.
/// </summary>
public sealed class OpenAIOptions
{
    /// <summary>
    /// Nome da seção de configuração da OpenAI.
    /// </summary>
    public const string SectionName = "OpenAI";

    /// <summary>
    /// Chave de acesso à OpenAI.
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Modelo utilizado nas operações de linguagem.
    /// </summary>
    [Required]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Tempo limite das requisições em segundos.
    /// </summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// Quantidade máxima de tokens da resposta.
    /// </summary>
    [Range(100, 4096)]
    public int MaxOutputTokens { get; set; }
}
