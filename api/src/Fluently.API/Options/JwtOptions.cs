using System.ComponentModel.DataAnnotations;

namespace Fluently.API.Options;

/// <summary>
/// Configurações de autenticação JWT.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// Nome da seção de configuração JWT.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Emissor dos tokens.
    /// </summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Público dos tokens.
    /// </summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Chave de assinatura dos tokens.
    /// </summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Tempo de expiração dos tokens em minutos.
    /// </summary>
    [Range(1, 1440)]
    public int ExpirationMinutes { get; set; }
}
