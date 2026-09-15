using Fluently.API.DTOs.Common;
using Fluently.API.DTOs.Leaderboard;
using Fluently.API.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fluently.API.Controllers.v1;

/// <summary>
/// Ranking global de experiência.
/// </summary>
[ApiController]
[Route("api/v1/leaderboard")]
[Authorize]
public sealed class LeaderboardController : ControllerBase
{
    /// <summary>
    /// Serviço de ranking.
    /// </summary>
    private readonly ILeaderboardService _leaderboardService;

    /// <summary>
    /// Inicializa uma nova instância do controlador do ranking.
    /// </summary>
    /// <param name="leaderboardService">Serviço utilizado para consultar o ranking.</param>
    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    /// <summary>
    /// Obtém uma página do ranking global.
    /// </summary>
    /// <param name="request">Parâmetros de paginação.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <response code="200">Retorna a página solicitada do ranking.</response>
    /// <response code="400">Os parâmetros de paginação são inválidos.</response>
    /// <response code="401">A autenticação é obrigatória.</response>
    [HttpGet(Name = "GetLeaderboard")]
    [ProducesResponseType<PaginatedResponseDTO<LeaderboardEntryResponseDTO>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResponseDTO<LeaderboardEntryResponseDTO>>> GetAsync([FromQuery] PaginationRequestDTO request,
                                                                                                CancellationToken cancellationToken)
    {
        var response = await _leaderboardService.GetAsync(request, cancellationToken);
        return Ok(response);
    }
}
