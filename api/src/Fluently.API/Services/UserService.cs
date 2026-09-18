using Fluently.API.DTOs.Users;
using Fluently.API.Exceptions;
using Fluently.API.Helpers;
using Fluently.API.Models;
using Fluently.API.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Fluently.API.Services;

/// <summary>
/// Dados do usuário autenticado.
/// </summary>
public sealed class UserService(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IQuestionRepository questionRepository,
    IPasswordHasher<UserModel> passwordHasher,
    ILogger<UserService> logger,
    TimeProvider timeProvider
) : IUserService
{
    /// <summary>
    /// Obtém os dados do usuário autenticado.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Dados públicos do usuário autenticado.</returns>
    public async Task<GetUserResponseDTO> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();
        var user = await GetUserAsync(userId, cancellationToken);

        return MapToGetResponse(user);
    }

    /// <summary>
    /// Atualiza o perfil do usuário.
    /// </summary>
    /// <param name="request">Dados do perfil que serão atualizados.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Dados atualizados do usuário.</returns>
    public async Task<UpdateUserResponseDTO> UpdateProfileAsync(
        UpdateUserRequestDTO request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUserService.GetUserId();
        var user = await GetUserAsync(userId, cancellationToken);

        if (request.FirstName is not null)
        {
            user.FirstName = request.FirstName.Trim();
        }

        if (request.LastName is not null)
        {
            user.LastName = request.LastName.Trim();
        }

        if (request.Proficiency.HasValue)
        {
            user.Proficiency = request.Proficiency.Value;
        }

        if (request.Bio is not null)
        {
            var updatedBio = request.Bio.Trim();
            if (!string.Equals(user.Bio, updatedBio, StringComparison.Ordinal))
            {
                user.Bio = updatedBio;
                await questionRepository.DeleteCurrentAsync(user.Id, cancellationToken);
            }
        }

        if (request.ProfileImageBase64 is not null)
        {
            user.ProfileImageBase64 = NormalizeProfileImage(request.ProfileImageBase64);
        }

        user.UpdatedAt = timeProvider.GetUtcNow();

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User profile updated. UserId: {UserId}", user.Id);

        return MapToUpdateResponse(user);
    }

    /// <summary>
    /// Atualiza as credenciais do usuário autenticado.
    /// </summary>
    /// <param name="request">Novo endereço de e-mail, nova senha e sua confirmação.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa que representa a operação assíncrona.</returns>
    public async Task UpdateCredentialsAsync(
        UpdateUserCredentialsRequestDTO request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUserService.GetUserId();
        var user = await GetUserAsync(userId, cancellationToken);

        await UpdateEmailAsync(user, request.Email, cancellationToken);

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        user.UpdatedAt = timeProvider.GetUtcNow();

        userRepository.Update(user);
        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User credentials updated. UserId: {UserId}", user.Id);
    }

    /// <summary>
    /// Atualiza o endereço de e-mail do usuário.
    /// </summary>
    /// <param name="user">Usuário que será atualizado.</param>
    /// <param name="email">Novo endereço de e-mail.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa que representa a operação assíncrona.</returns>
    private async Task UpdateEmailAsync(UserModel user, string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = EmailNormalizerHelper.Normalize(email);

        if (
            normalizedEmail != user.NormalizedEmail
            && await userRepository.ExistsByNormalizedEmailAsync(normalizedEmail, cancellationToken)
        )
        {
            throw new ConflictException("Já existe uma conta cadastrada com este e-mail.");
        }

        user.Email = email.Trim();
        user.NormalizedEmail = normalizedEmail;
    }

    /// <summary>
    /// Obtém um usuário pelo identificador.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Usuário encontrado.</returns>
    private async Task<UserModel> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        return user ?? throw new NotFoundException("O usuário autenticado não foi encontrado.");
    }

    /// <summary>
    /// Normaliza e valida uma imagem de perfil codificada em Base64.
    /// </summary>
    /// <param name="profileImageBase64">Imagem de perfil informada pelo usuário.</param>
    /// <returns>Imagem normalizada ou valor nulo quando nenhuma imagem foi informada.</returns>
    private static string? NormalizeProfileImage(string profileImageBase64)
    {
        var value = profileImageBase64.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        try
        {
            var imageBytes = Convert.FromBase64String(value);
            if (imageBytes.Length > 750000)
            {
                throw new BadRequestException("A imagem de perfil é muito grande.");
            }

            return Convert.ToBase64String(imageBytes);
        }
        catch (FormatException)
        {
            throw new BadRequestException("A imagem de perfil é inválida.");
        }
    }

    /// <summary>
    /// Converte o usuário para sua resposta pública.
    /// </summary>
    /// <param name="user">Usuário que será convertido.</param>
    /// <returns>Dados públicos do usuário.</returns>
    private static GetUserResponseDTO MapToGetResponse(UserModel user)
    {
        return new GetUserResponseDTO
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            TotalXp = user.TotalXp,
            CurrentStreak = user.CurrentStreak,
            Proficiency = user.Proficiency,
            Bio = user.Bio,
            ProfileImageBase64 = user.ProfileImageBase64,
            CreatedAt = user.CreatedAt,
        };
    }

    /// <summary>
    /// Converte o usuário para os dados retornados após sua atualização.
    /// </summary>
    /// <param name="user">Usuário que será convertido.</param>
    /// <returns>Dados atualizados do usuário.</returns>
    private static UpdateUserResponseDTO MapToUpdateResponse(UserModel user)
    {
        return new UpdateUserResponseDTO
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            TotalXp = user.TotalXp,
            CurrentStreak = user.CurrentStreak,
            Proficiency = user.Proficiency,
            Bio = user.Bio,
            ProfileImageBase64 = user.ProfileImageBase64,
            CreatedAt = user.CreatedAt,
        };
    }
}
