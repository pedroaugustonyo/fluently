using Fluently.API.DTOs.Users;
using Fluently.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fluently.API.Controllers.v1;

/// <summary>
/// Endpoints de usuários.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// Obtém os dados do usuário.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <response code="200">Retorna os dados e o progresso do usuário.</response>
    /// <response code="401">A autenticação é obrigatória.</response>
    /// <response code="404">O usuário autenticado não foi encontrado.</response>
    [HttpGet("me", Name = "GetCurrentUser")]
    [ProducesResponseType<GetUserResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetUserResponseDTO>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var response = await userService.GetCurrentAsync(cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Atualiza nome, nível de proficiência e biografia.
    /// </summary>
    /// <param name="request">Dados do perfil que serão atualizados.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <response code="200">Retorna os dados atualizados do usuário.</response>
    /// <response code="400">Os dados informados são inválidos.</response>
    /// <response code="401">A autenticação é obrigatória.</response>
    /// <response code="404">O usuário autenticado não foi encontrado.</response>
    [HttpPut("me", Name = "UpdateUserProfile")]
    [ProducesResponseType<UpdateUserResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateUserResponseDTO>> UpdateProfileAsync(
        [FromBody] UpdateUserRequestDTO request,
        CancellationToken cancellationToken
    )
    {
        var response = await userService.UpdateProfileAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Atualiza as credenciais do usuário.
    /// </summary>
    /// <param name="request">Novo endereço de e-mail, nova senha e sua confirmação.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <response code="204">As credenciais foram atualizadas.</response>
    /// <response code="400">Os dados informados são inválidos.</response>
    /// <response code="401">A autenticação é obrigatória.</response>
    /// <response code="404">O usuário autenticado não foi encontrado.</response>
    /// <response code="409">O e-mail informado já pertence a outra conta.</response>
    [HttpPut("me/credentials", Name = "UpdateUserCredentials")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCredentialsAsync(
        [FromBody] UpdateUserCredentialsRequestDTO request,
        CancellationToken cancellationToken
    )
    {
        await userService.UpdateCredentialsAsync(request, cancellationToken);
        return NoContent();
    }
}
