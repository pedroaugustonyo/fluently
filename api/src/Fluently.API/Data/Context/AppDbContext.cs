using Fluently.API.Models;

using Microsoft.EntityFrameworkCore;

namespace Fluently.API.Data.Context;

/// <summary>
/// Sessão de acesso ao banco de dados da aplicação.
/// </summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>
    /// Inicializa uma nova instância do contexto da aplicação.
    /// </summary>
    /// <param name="options">Opções utilizadas para configurar o contexto.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Conjunto de usuários.
    /// </summary>
    public DbSet<UserModel> Users => Set<UserModel>();

    /// <summary>
    /// Conjunto de questões.
    /// </summary>
    public DbSet<QuestionModel> Questions => Set<QuestionModel>();

    /// <summary>
    /// Persiste as alterações e atualiza os dados comuns das entidades.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Quantidade de registros afetados.</returns>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetUpdatedAt();

        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Aplica as configurações das entidades ao modelo do banco de dados.
    /// </summary>
    /// <param name="modelBuilder">Construtor utilizado para configurar o modelo.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <summary>
    /// Define a data de atualização das entidades modificadas.
    /// </summary>
    private void SetUpdatedAt()
    {
        var now = DateTimeOffset.UtcNow;
        var modifiedEntries = ChangeTracker.Entries<BaseModel>()
            .Where(entry => entry.State == EntityState.Modified);

        foreach (var entry in modifiedEntries)
        {
            entry.Entity.UpdatedAt = now;
        }
    }
}
