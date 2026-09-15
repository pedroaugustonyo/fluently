using Fluently.API.DTOs.Common;
using Fluently.API.DTOs.Questions;
using Fluently.API.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fluently.API.Controllers.v1;

/// <summary>
/// Fluxo contínuo de questões e respostas.
/// </summary>
[ApiController]
[Route("api/v1/questions")]
[Authorize]
public sealed class QuestionsController : ControllerBase
{
    /// <summary>
    /// Serviço de questões.
    /// </summary>
    private readonly IQuestionService _questionService;

    /// <summary>
    /// Inicializa uma nova instância do controlador de questões.
    /// </summary>
    /// <param name="questionService">Serviço utilizado no fluxo de exercícios.</param>
    public QuestionsController(IQuestionService questionService)
    {
        _questionService = questionService;
    }

    /// <summary>
    /// Obtém a questão atual.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <response code="200">Retorna a questão pendente.</response>
    /// <response code="401">A autenticação é obrigatória.</response>
    /// <response code="404">Não existe uma questão pendente.</response>
    [HttpGet("current", Name = "GetCurrentQuestion")]
    [ProducesResponseType<QuestionResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionResponseDTO>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var response = await _questionService.GetCurrentAsync(cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Obtém uma página de questões.
    /// </summary>
    /// <param name="request">Parâmetros de paginação.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <response code="200">Retorna a página de questões do usuário.</response>
    /// <response code="400">Os parâmetros de paginação são inválidos.</response>
    /// <response code="401">A autenticação é obrigatória.</response>
    [HttpGet(Name = "GetQuestions")]
    [ProducesResponseType<PaginatedResponseDTO<QuestionDetailsResponseDTO>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResponseDTO<QuestionDetailsResponseDTO>>> GetAllAsync([FromQuery] PaginationRequestDTO request,
                                                                                                  CancellationToken cancellationToken)
    {
        var response = await _questionService.GetAllAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Obtém uma questão pelo ID.
    /// </summary>
    /// <param name="id">Identificador da questão.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <response code="200">Retorna a questão solicitada.</response>
    /// <response code="401">A autenticação é obrigatória.</response>
    /// <response code="404">A questão não foi encontrada.</response>
    [HttpGet("{id:guid}", Name = "GetQuestionById")]
    [ProducesResponseType<QuestionDetailsResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionDetailsResponseDTO>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _questionService.GetByIdAsync(id, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Gera uma nova questão.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <response code="201">Retorna a questão criada.</response>
    /// <response code="400">O contexto de aprendizagem ainda não foi preenchido.</response>
    /// <response code="401">A autenticação é obrigatória.</response>
    /// <response code="409">Já existe uma questão pendente.</response>
    /// <response code="503">O serviço de geração está temporariamente indisponível.</response>
    [HttpPost(Name = "GenerateQuestion")]
    [ProducesResponseType<QuestionResponseDTO>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<QuestionResponseDTO>> CreateAsync(CancellationToken cancellationToken)
    {
        var response = await _questionService.CreateAsync(cancellationToken);
        return Created($"/api/v1/questions/{response.Id}", response);
    }

    /// <summary>
    /// Registra a alternativa selecionada para uma questão.
    /// </summary>
    /// <param name="id">Identificador da questão.</param>
    /// <param name="request">Resposta selecionada pelo usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <response code="201">Retorna o resultado da resposta.</response>
    /// <response code="400">A resposta informada é inválida.</response>
    /// <response code="401">A autenticação é obrigatória.</response>
    /// <response code="404">A questão não foi encontrada.</response>
    /// <response code="409">A questão já foi respondida.</response>
    [HttpPost("{id:guid}", Name = "SubmitQuestionAnswer")]
    [ProducesResponseType<QuestionAnswerResponseDTO>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuestionAnswerResponseDTO>> SubmitAnswerAsync(Guid id,
                                                                                 [FromBody] SubmitQuestionAnswerRequestDTO request,
                                                                                 CancellationToken cancellationToken)
    {
        var response = await _questionService.SubmitAnswerAsync(id, request, cancellationToken);
        return Created($"/api/v1/questions/{id}", response);
    }
}
