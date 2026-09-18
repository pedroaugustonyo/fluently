namespace Fluently.API.Models;

/// <summary>
/// Campos comuns das entidades persistidas.
/// </summary>
public abstract class BaseModel
{
    /// <summary>
    /// Identificador único.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Data de criação.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Data da última atualização.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
