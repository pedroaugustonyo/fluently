using Fluently.API.DTOs.Common;
using Fluently.API.DTOs.Tasks;
using Fluently.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fluently.API.Controllers.v1;

/// <summary>
/// Gerencia as tarefas do usuário autenticado.
/// </summary>
[ApiController]
[Route("api/v1/tasks")]
[Authorize]
public sealed class TasksController(ITaskService taskService) : ControllerBase
{
    /// <summary>
    /// Obtém uma tarefa.
    /// </summary>
    [HttpGet("{id:guid}", Name = "GetTaskById")]
    public async Task<ActionResult<TaskResponseDTO>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await taskService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>
    /// Obtém as tarefas do usuário autenticado.
    /// </summary>
    /// <param name="request">Parâmetros de paginação e busca.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefas encontradas.</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponseDTO<TaskResponseDTO>>> PaginateAsync(
        [FromQuery] PaginationRequestDTO request,
        CancellationToken cancellationToken
    )
    {
        return Ok(await taskService.PaginateAsync(request, cancellationToken));
    }

    /// <summary>
    /// Cria uma tarefa.
    /// </summary>
    /// <param name="request">Dados da tarefa.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa criada.</returns>
    [HttpPost]
    public async Task<ActionResult<TaskResponseDTO>> CreateAsync(
        CreateTaskRequestDTO request,
        CancellationToken cancellationToken
    )
    {
        var response = await taskService.CreateAsync(request, cancellationToken);

        return CreatedAtRoute("GetTaskById", new { id = response.Id }, response);
    }

    /// <summary>
    /// Atualiza uma tarefa.
    /// </summary>
    /// <param name="id">Identificador da tarefa.</param>
    /// <param name="request">Dados atualizados da tarefa.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa atualizada.</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskResponseDTO>> UpdateAsync(
        Guid id,
        UpdateTaskRequestDTO request,
        CancellationToken cancellationToken
    )
    {
        return Ok(await taskService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>
    /// Atualiza a conclusão de uma tarefa.
    /// </summary>
    /// <param name="id">Identificador da tarefa.</param>
    /// <param name="request">Estado de conclusão.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa atualizada.</returns>
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<TaskResponseDTO>> SetCompletionAsync(
        Guid id,
        TaskCompletionRequestDTO request,
        CancellationToken cancellationToken
    )
    {
        return Ok(await taskService.SetCompletionAsync(id, request, cancellationToken));
    }

    /// <summary>
    /// Exclui uma tarefa.
    /// </summary>
    /// <param name="id">Identificador da tarefa.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Resposta sem conteúdo.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await taskService.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
