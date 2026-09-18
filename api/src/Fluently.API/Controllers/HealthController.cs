using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fluently.API.Controllers;

/// <summary>
/// Endpoints de integridade da aplicação.
/// </summary>
[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    /// <summary>
    /// Inicializa uma nova instância do controlador de integridade.
    /// </summary>
    /// <param name="healthCheckService">Serviço que executa as verificações de integridade.</param>
    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Verifica se o processo da API está em execução.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Resposta de disponibilidade do processo.</returns>
    [HttpGet("live", Name = "GetLiveness")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetLiveAsync(CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(_ => false, cancellationToken);

        return ToActionResult(report.Status);
    }

    /// <summary>
    /// Verifica se as dependências da API estão disponíveis.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Resposta de prontidão da aplicação.</returns>
    [HttpGet("ready", Name = "GetReadiness")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReadyAsync(CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(cancellationToken);

        return ToActionResult(report.Status);
    }

    private IActionResult ToActionResult(HealthStatus status)
    {
        return status == HealthStatus.Unhealthy ? StatusCode(StatusCodes.Status503ServiceUnavailable) : Ok();
    }
}
