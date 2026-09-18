using Fluently.API.DTOs.Tasks;
using Fluently.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fluently.API.Controllers.v1;

/// <summary>
/// Expõe a meta diária de experiência do usuário.
/// </summary>
[ApiController]
[Route("api/v1/daily-xp-goal")]
[Authorize]
public sealed class DailyXpGoalController(IDailyXpGoalService dailyXpGoalService) : ControllerBase
{
    /// <summary>
    /// Obtém a meta diária e o progresso atual.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Meta diária e progresso atual.</returns>
    [HttpGet]
    public async Task<ActionResult<DailyXpGoalResponseDTO>> GetAsync(CancellationToken cancellationToken)
    {
        return Ok(await dailyXpGoalService.GetAsync(cancellationToken));
    }

    /// <summary>
    /// Define a meta diária de experiência.
    /// </summary>
    /// <param name="request">Meta diária informada pelo usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Meta diária e progresso atualizados.</returns>
    [HttpPut]
    public async Task<ActionResult<DailyXpGoalResponseDTO>> UpdateAsync(
        UpdateDailyXpGoalRequestDTO request,
        CancellationToken cancellationToken
    )
    {
        return Ok(await dailyXpGoalService.UpdateAsync(request, cancellationToken));
    }
}
