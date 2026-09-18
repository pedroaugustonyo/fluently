using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fluently.API.Data.Context;

/// <summary>
/// Fábrica do contexto do banco de dados para ferramentas de desenvolvimento.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// Cria um contexto configurado a partir das variáveis de ambiente.
    /// </summary>
    /// <param name="args">Argumentos fornecidos pela ferramenta de desenvolvimento.</param>
    /// <returns>Contexto configurado para acesso ao banco de dados.</returns>
    public AppDbContext CreateDbContext(string[] args)
    {
        Env.NoClobber().TraversePath().Load(Path.Combine(AppContext.BaseDirectory, ".env"));

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings__DefaultConnection must be configured.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options;

        return new AppDbContext(options, TimeProvider.System);
    }
}
