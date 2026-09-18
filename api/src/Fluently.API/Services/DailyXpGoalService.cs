using Fluently.API.DTOs.Tasks;
using Fluently.API.Exceptions;
using Fluently.API.Models;
using Fluently.API.Repositories;

namespace Fluently.API.Services;

/// <summary>
/// Implementa as regras de negócio da meta diária de experiência.
/// </summary>
public sealed class DailyXpGoalService(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IQuestionRepository questionRepository,
    TimeProvider timeProvider
) : IDailyXpGoalService
{
    /// <summary>
    /// Identificador da zona de horário de Brasília.
    /// </summary>
    private const string BrasiliaTimeZoneId = "America/Sao_Paulo";

    /// <summary>
    /// Obtém a meta diária e o progresso do usuário autenticado.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Meta diária e progresso atual do usuário.</returns>
    public async Task<DailyXpGoalResponseDTO> GetAsync(CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(cancellationToken);

        return await BuildResponseAsync(user.DailyXpGoal, cancellationToken);
    }

    /// <summary>
    /// Atualiza a meta diária do usuário autenticado.
    /// </summary>
    /// <param name="request">Nova meta diária de experiência.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Meta diária e progresso atualizados.</returns>
    public async Task<DailyXpGoalResponseDTO> UpdateAsync(
        UpdateDailyXpGoalRequestDTO request,
        CancellationToken cancellationToken
    )
    {
        var user = await GetUserAsync(cancellationToken);

        user.DailyXpGoal = request.TargetXp;

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(cancellationToken);

        return await BuildResponseAsync(user.DailyXpGoal, cancellationToken);
    }

    /// <summary>
    /// Obtém o usuário autenticado.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Usuário autenticado.</returns>
    private async Task<UserModel> GetUserAsync(CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUserService.GetUserId(), cancellationToken);

        return user ?? throw new NotFoundException("O usuário não foi encontrado.");
    }

    /// <summary>
    /// Monta o contrato de resposta com a experiência obtida no dia atual em Brasília.
    /// </summary>
    /// <param name="targetXp">Meta diária configurada pelo usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Contrato de resposta da meta diária.</returns>
    private async Task<DailyXpGoalResponseDTO> BuildResponseAsync(int? targetXp, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var brasiliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(BrasiliaTimeZoneId);
        var brasiliaNow = TimeZoneInfo.ConvertTime(now, brasiliaTimeZone);
        var startOfDay = new DateTimeOffset(brasiliaNow.Date, brasiliaNow.Offset).ToUniversalTime();
        var startOfNextDay = startOfDay.AddDays(1);
        var userId = currentUserService.GetUserId();
        var earnedXp = await questionRepository.GetAwardedXpAsync(
            userId,
            startOfDay,
            startOfNextDay,
            cancellationToken
        );

        return new DailyXpGoalResponseDTO
        {
            TargetXp = targetXp,
            EarnedXp = earnedXp,
            IsCompleted = targetXp.HasValue && earnedXp >= targetXp.Value,
        };
    }
}
