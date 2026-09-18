using System.ComponentModel.DataAnnotations;

namespace Fluently.API.DTOs.Tasks;

/// <summary>
/// Meta diária de experiência definida pelo usuário.
/// </summary>
public sealed class UpdateDailyXpGoalRequestDTO
{
    /// <summary>
    /// Quantidade de experiência desejada.
    /// </summary>
    [Range(1, 1000)]
    public int TargetXp { get; init; }
}
