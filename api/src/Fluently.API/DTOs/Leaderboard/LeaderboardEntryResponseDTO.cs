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
    /// Experiência total acumulada pelo usuário.
    /// </summary>
    public long TotalXp { get; init; }
}
