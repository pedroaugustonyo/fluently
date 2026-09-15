using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Fluently.API.Data.Context;
using Fluently.API.Filters;
using Fluently.API.Helpers;
using Fluently.API.Middleware;
using Fluently.API.Models;
using Fluently.API.Options;
using Fluently.API.Repositories;
using Fluently.API.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using OpenAI.Chat;

namespace Fluently.API;

/// <summary>
/// Configuração e inicialização da aplicação.
/// </summary>
public static class IOC
{
    /// <summary>
    /// Registra os componentes necessários para executar a aplicação.
    /// </summary>
    /// <param name="builder">Construtor da aplicação Web.</param>
    /// <returns>Construtor configurado da aplicação Web.</returns>
    public static WebApplicationBuilder AddApplication(this WebApplicationBuilder builder)
    {
        ConfigureLogging(builder);
        ConfigureApi(builder);
        ConfigureExceptionHandling(builder);

        ConfigureOptions(builder);

        ConfigureDatabase(builder);
        ConfigureServices(builder);
        ConfigureHealthChecks(builder);
        ConfigureAuthentication(builder);

        return builder;
    }

    /// <summary>
    /// Configura o pipeline HTTP da aplicação.
    /// </summary>
    /// <param name="app">Aplicação Web que será configurada.</param>
    /// <returns>Aplicação Web configurada.</returns>
    public static WebApplication UseApplication(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages(WriteStatusCodeProblemDetailsAsync);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fluently API v1");
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    /// <summary>
    /// Aplica as migrações pendentes antes de iniciar a aplicação.
    /// </summary>
    /// <param name="app">Aplicação que fornecerá o contexto do banco de dados.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Aplicação com o banco de dados atualizado.</returns>
    public static async Task<WebApplication> ApplyDatabaseMigrationsAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);

        return app;
    }

    /// <summary>
    /// Mapeia os controladores e as rotas de diagnóstico da aplicação.
    /// </summary>
    /// <param name="app">Aplicação Web que receberá as rotas.</param>
    /// <returns>Aplicação Web com as rotas mapeadas.</returns>
    public static WebApplication MapApplicationRoutes(this WebApplication app)
    {
        app.MapControllers();

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .WithTags("Sistema")
            .WithName("Disponibilidade")
            .WithDescription("Verifica se o processo da API está em execução.");

        app.MapHealthChecks("/health/ready", new HealthCheckOptions())
            .WithTags("Sistema")
            .WithName("Prontidão")
            .WithDescription("Verifica se as dependências da API estão disponíveis.");

        return app;
    }

    /// <summary>
    /// Configura a saída legível dos logs no console.
    /// </summary>
    /// <param name="builder">Construtor da aplicação Web.</param>
    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options =>
        {
            options.TimestampFormat = "HH:mm:ss ";
            options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
            options.IncludeScopes = true;
        });
    }

    /// <summary>
    /// Configura controladores, serialização e documentação da API.
    /// </summary>
    /// <param name="builder">Construtor da aplicação Web.</param>
    private static void ConfigureApi(WebApplicationBuilder builder)
    {
        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition =
                    JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Insert(
                    0,
                    new TrimStringJsonConverterHelper());
            });

        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = CreateValidationProblemResponse;
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Fluently.API",
                Version = "v1",
                Description = "API principal do app Fluently - Plataforma de aprendizagem contínua de inglês para estudantes brasileiros."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Informe o token JWT de acesso."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = []
            });

            var xmlFileName = $"{typeof(IOC).Assembly.GetName().Name}.xml";
            var xmlFilePath = Path.Combine(AppContext.BaseDirectory, xmlFileName);

            options.IncludeXmlComments(xmlFilePath);
            options.SchemaFilter<EnumSchemaFilter>();
        });
    }

    /// <summary>
    /// Configura o tratamento centralizado de exceções e problemas HTTP.
    /// </summary>
    /// <param name="builder">Construtor da aplicação Web.</param>
    private static void ConfigureExceptionHandling(WebApplicationBuilder builder)
    {
        builder.Services.AddExceptionHandler<ApiExceptionHandlerMiddleware>();
        builder.Services.AddProblemDetails();
    }

    /// <summary>
    /// Registra e valida as configurações tipadas da aplicação.
    /// </summary>
    /// <param name="builder">Construtor da aplicação Web.</param>
    private static void ConfigureOptions(WebApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<JwtOptions>()
            .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services
            .AddOptions<OpenAIOptions>()
            .Bind(builder.Configuration.GetSection(OpenAIOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    /// <summary>
    /// Configura o contexto e a conexão com o banco de dados.
    /// </summary>
    /// <param name="builder">Construtor da aplicação Web.</param>
    private static void ConfigureDatabase(WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must be configured.");
        }

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(builder.Environment.IsDevelopment());
        });
    }

    /// <summary>
    /// Registra os repositórios, serviços e clientes da aplicação.
    /// </summary>
    /// <param name="builder">Construtor da aplicação Web.</param>
    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();

        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IQuestionService, QuestionService>();
        builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
        builder.Services.AddScoped<IQuestionGenerationService, QuestionGenerationService>();

        builder.Services.AddSingleton<IChatClient>(serviceProvider =>
        {
            var openAIOptions = serviceProvider.GetRequiredService<IOptions<OpenAIOptions>>().Value;
            var chatClient = new ChatClient(openAIOptions.Model, openAIOptions.ApiKey);

            return chatClient.AsIChatClient();
        });
        builder.Services.AddSingleton<ILanguageModelClient, LanguageModelClient>();
    }

    /// <summary>
    /// Configura as verificações de integridade da aplicação.
    /// </summary>
    /// <param name="builder">Construtor da aplicação Web.</param>
    private static void ConfigureHealthChecks(WebApplicationBuilder builder)
    {
        builder.Services
            .AddHealthChecks()
            .AddDbContextCheck<AppDbContext>();
    }

    /// <summary>
    /// Configura a emissão e a validação dos tokens de autenticação.
    /// </summary>
    /// <param name="builder">Construtor da aplicação Web.</param>
    private static void ConfigureAuthentication(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IPasswordHasher<UserModel>, PasswordHasher<UserModel>>();
        builder.Services.AddSingleton<ITokenService, JwtTokenService>();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        builder.Services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) =>
            {
                var jwtOptions = jwtOptionsAccessor.Value;

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        builder.Services.AddAuthorization();
    }

    /// <summary>
    /// Cria a resposta padronizada para erros de validação do modelo.
    /// </summary>
    /// <param name="context">Contexto da ação que apresentou erros de validação.</param>
    /// <returns>Resposta HTTP contendo os erros localizados.</returns>
    private static IActionResult CreateValidationProblemResponse(ActionContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString()
            ?? context.HttpContext.TraceIdentifier;
        var errors = GetLocalizedValidationErrors(context);
        var problemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest),
            Detail = "Corrija os campos informados e tente novamente.",
            Instance = context.HttpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;

        var result = new BadRequestObjectResult(problemDetails);
        result.ContentTypes.Add("application/problem+json");

        return result;
    }

    /// <summary>
    /// Obtém os erros de validação com mensagens em português do Brasil.
    /// </summary>
    /// <param name="context">Contexto da ação que apresentou erros de validação.</param>
    /// <returns>Dicionário de erros agrupados por campo.</returns>
    private static Dictionary<string, string[]> GetLocalizedValidationErrors(ActionContext context)
    {
        return context.ModelState
            .Where(entry => entry.Value is not null && entry.Value.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(GetLocalizedValidationError)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray());
    }

    /// <summary>
    /// Converte mensagens internas de validação para português do Brasil.
    /// </summary>
    /// <param name="error">Erro de validação que será localizado.</param>
    /// <returns>Mensagem apropriada para a resposta da API.</returns>
    private static string GetLocalizedValidationError(ModelError error)
    {
        if (error.Exception is JsonException)
        {
            return "O corpo da solicitação possui um formato JSON inválido.";
        }

        if (error.ErrorMessage == "The request field is required.")
        {
            return "O corpo da solicitação é obrigatório.";
        }

        return string.IsNullOrWhiteSpace(error.ErrorMessage)
            ? "O valor informado é inválido."
            : error.ErrorMessage;
    }

    /// <summary>
    /// Escreve detalhes padronizados para respostas HTTP sem conteúdo.
    /// </summary>
    /// <param name="context">Contexto da resposta gerada pelo código de status.</param>
    /// <returns>Tarefa que representa a escrita assíncrona da resposta.</returns>
    private static async Task WriteStatusCodeProblemDetailsAsync(StatusCodeContext context)
    {
        var httpContext = context.HttpContext;

        if (httpContext.Response.HasStarted || httpContext.Response.ContentLength.HasValue)
        {
            return;
        }

        var problemDetailsService =
            httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        var statusCode = httpContext.Response.StatusCode;
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        var problemDetails = CreateStatusCodeProblemDetails(
            statusCode,
            httpContext.Request.Path,
            traceId);

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }

    /// <summary>
    /// Cria os detalhes públicos correspondentes a um código de status HTTP.
    /// </summary>
    /// <param name="statusCode">Código de status HTTP da resposta.</param>
    /// <param name="instance">Caminho da requisição associada ao erro.</param>
    /// <param name="traceId">Identificador de rastreamento da requisição.</param>
    /// <returns>Detalhes padronizados do problema.</returns>
    private static ProblemDetails CreateStatusCodeProblemDetails(int statusCode,
                                                                 string instance,
                                                                 string traceId)
    {
        var detail = statusCode switch
        {
            StatusCodes.Status400BadRequest =>
                "A requisição não pôde ser processada.",
            StatusCodes.Status401Unauthorized =>
                "A autenticação é obrigatória para acessar este recurso.",
            StatusCodes.Status403Forbidden =>
                "Você não possui permissão para acessar este recurso.",
            StatusCodes.Status404NotFound =>
                "O recurso solicitado não foi encontrado.",
            StatusCodes.Status409Conflict =>
                "A requisição entra em conflito com o estado atual do recurso.",
            _ => "Tente novamente em instantes."
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = detail,
            Instance = instance
        };

        problemDetails.Extensions["traceId"] = traceId;

        return problemDetails;
    }
}
