using Fluently.API.DTOs.Users;
using Fluently.API.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fluently.API.Controllers.v1;

/// <summary>
/// Endpoints de autenticação.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    /// <summary>
    /// Serviço de autenticação.
    /// </summary>
    private readonly IAuthService _authService;

    /// <summary>
    /// Inicializa uma nova instância do controlador de autenticação.
    /// </summary>
    /// <param name="authService">Serviço utilizado nas operações de autenticação.</param>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Cadastra uma nova conta de usuário.
    /// </summary>
    /// <param name="request">Dados necessários para o cadastro.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <response code="201">Retorna a conta criada.</response>
    /// <response code="400">Os dados informados são inválidos.</response>
    /// <response code="409">Já existe uma conta com o e-mail informado.</response>
    [HttpPost("register", Name = "Register")]
    [ProducesResponseType<CreateUserResponseDTO>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateUserResponseDTO>> RegisterAsync([FromBody] CreateUserRequestDTO request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, cancellationToken);
        return Created("/api/v1/users/me", response);
    }

    /// <summary>
    /// Autentica um usuário com e-mail e senha.
    /// </summary>
    /// <param name="request">Credenciais utilizadas para autenticação.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <response code="200">Retorna o token de acesso e os dados do usuário.</response>
    /// <response code="400">Os dados informados são inválidos.</response>
    /// <response code="401">As credenciais são inválidas.</response>
    [HttpPost("login", Name = "Login")]
    [ProducesResponseType<LoginUserResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginUserResponseDTO>> LoginAsync([FromBody] LoginUserRequestDTO request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }
}
