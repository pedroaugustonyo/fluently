using Fluently.API.DTOs.Common;
using Fluently.API.DTOs.Leaderboard;
using Fluently.API.Helpers;
using Fluently.API.Repositories;

namespace Fluently.API.Services;

/// <summary>
/// Ranking de usuários por experiência acumulada.
/// </summary>
public sealed class LeaderboardService(IUserRepository userRepository) : ILeaderboardService
{
    /// <summary>
    /// Obtém uma página do ranking ordenado pela experiência acumulada.
    /// </summary>
    /// <param name="request">Parâmetros de paginação da consulta.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Página com as posições do ranking.</returns>
    public async Task<PaginatedResponseDTO<LeaderboardEntryResponseDTO>> PaginateAsync(
        PaginationRequestDTO request,
        CancellationToken cancellationToken
    )
    {
        var skip = PaginationHelper.CalculateSkip(request);
        var totalItems = await userRepository.CountLeaderboardAsync(request.Search, cancellationToken);
        var users = await userRepository.GetLeaderboardPageAsync(
            skip,
            request.PageSize,
            request.Search,
            cancellationToken
        );
        var entries = users
            .Select(
                (user, index) =>
                    new LeaderboardEntryResponseDTO
                    {
                        Rank = skip + index + 1,
                        UserId = user.Id,
                        FullName = $"{user.FirstName} {user.LastName}".Trim(),
                        ProfileImageBase64 = user.ProfileImageBase64,
                        TotalXp = user.TotalXp,
                    }
            )
            .ToArray();

        return PaginationHelper.CreateResponse(entries, request, totalItems);
    }
}
