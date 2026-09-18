using Fluently.API.DTOs.Users;
using Fluently.API.Exceptions;
using Fluently.API.Helpers;
using Fluently.API.Models;
using Fluently.API.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Fluently.API.Services;

/// <summary>
/// Cadastro e autenticação de usuários.
/// </summary>
public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher<UserModel> passwordHasher,
    ITokenService tokenService,
    ILogger<AuthService> logger
) : IAuthService
{
    /// <summary>
    /// Cadastra uma nova conta de usuário.
    /// </summary>
    /// <param name="request">Dados necessários para o cadastro.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Dados públicos do usuário cadastrado.</returns>
    public async Task<CreateUserResponseDTO> RegisterAsync(
        CreateUserRequestDTO request,
        CancellationToken cancellationToken
    )
    {
        var normalizedEmail = EmailNormalizerHelper.Normalize(request.Email);

        if (await userRepository.ExistsByNormalizedEmailAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException("Já existe uma conta cadastrada com este e-mail.");
        }

        var user = new UserModel
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = string.Empty,
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User registered. UserId: {UserId}", user.Id);

        return MapToCreateResponse(user);
    }

    /// <summary>
    /// Autentica um usuário com e-mail e senha.
    /// </summary>
    /// <param name="request">Credenciais utilizadas na autenticação.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Token de acesso e dados do usuário autenticado.</returns>
    public async Task<LoginUserResponseDTO> LoginAsync(LoginUserRequestDTO request, CancellationToken cancellationToken)
    {
        var normalizedEmail = EmailNormalizerHelper.Normalize(request.Email);
        var user = await userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (
            user is null
            || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed
        )
        {
            logger.LogWarning("Authentication rejected. {AuthenticationFailureReason}", "InvalidCredentials");

            throw new UnauthorizedException("E-mail ou senha inválidos.");
        }

        var token = tokenService.Create(user.Id, user.Email);
        logger.LogInformation("User authenticated. UserId: {UserId}", user.Id);

        return new LoginUserResponseDTO
        {
            AccessToken = token.AccessToken,
            TokenType = "Bearer",
            ExpiresAt = token.ExpiresAt,
            User = MapToLoginUserResponse(user),
        };
    }

    /// <summary>
    /// Converte o usuário para sua resposta pública.
    /// </summary>
    /// <param name="user">Usuário que será convertido.</param>
    /// <returns>Dados públicos do usuário.</returns>
    private static CreateUserResponseDTO MapToCreateResponse(UserModel user)
    {
        return new CreateUserResponseDTO
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
    /// Converte o usuário para os dados retornados na autenticação.
    /// </summary>
    /// <param name="user">Usuário que será convertido.</param>
    /// <returns>Dados públicos do usuário autenticado.</returns>
    private static LoginUserDetailsResponseDTO MapToLoginUserResponse(UserModel user)
    {
        return new LoginUserDetailsResponseDTO
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
