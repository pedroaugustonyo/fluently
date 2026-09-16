namespace Fluently.API.DTOs.Leaderboard;

/// <summary>
/// Posição no ranking global.
/// </summary>
public sealed class LeaderboardEntryResponseDTO
{
    /// <summary>
    /// Posição do usuário no ranking.
    /// </summary>
    public int Rank { get; init; }

    /// <summary>
    /// Identificador do usuário.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Nome completo exibido no ranking.
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Imagem de perfil codificada em Base64.
    /// </summary>
    public string? ProfileImageBase64 { get; init; }

    /// <summary>
    /// Experiência total acumulada pelo usuário.
    /// </summary>
    public long TotalXp { get; init; }
}
