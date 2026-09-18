namespace Fluently.API.DTOs.Tasks;

/// <summary>
/// Progresso da meta diária de experiência.
/// </summary>
public sealed class DailyXpGoalResponseDTO
{
    /// <summary>
    /// Meta diária definida.
    /// </summary>
    public int? TargetXp { get; init; }

    /// <summary>
    /// Experiência obtida hoje.
    /// </summary>
    public int EarnedXp { get; init; }

    /// <summary>
    /// Indica se a meta foi concluída.
    /// </summary>
    public bool IsCompleted { get; init; }
}
