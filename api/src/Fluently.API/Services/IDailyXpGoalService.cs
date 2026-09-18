using Fluently.API.DTOs.Tasks;

namespace Fluently.API.Services;

/// <summary>
/// Define as operações de negócio da meta diária de experiência.
/// </summary>
public interface IDailyXpGoalService
{
    /// <summary>
    /// Obtém a meta diária e o progresso do usuário autenticado.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Meta diária e progresso atual do usuário.</returns>
    Task<DailyXpGoalResponseDTO> GetAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Atualiza a meta diária do usuário autenticado.
    /// </summary>
    /// <param name="request">Nova meta diária de experiência.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Meta diária e progresso atualizados.</returns>
    Task<DailyXpGoalResponseDTO> UpdateAsync(UpdateDailyXpGoalRequestDTO request, CancellationToken cancellationToken);
}
