using Fluently.API.DTOs.Common;
using Fluently.API.DTOs.Leaderboard;

namespace Fluently.API.Services;

/// <summary>
/// Operações de consulta ao ranking de usuários.
/// </summary>
public interface ILeaderboardService
{
    /// <summary>
    /// Obtém uma página do ranking ordenado pela experiência acumulada.
    /// </summary>
    /// <param name="request">Parâmetros de paginação da consulta.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Página com as posições do ranking.</returns>
    Task<PaginatedResponseDTO<LeaderboardEntryResponseDTO>> GetAsync(PaginationRequestDTO request,
                                                                     CancellationToken cancellationToken);
}
