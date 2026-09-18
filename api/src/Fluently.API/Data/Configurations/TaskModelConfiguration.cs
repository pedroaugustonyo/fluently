using Fluently.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluently.API.Data.Configurations;

/// <summary>
/// Configura a persistência das tarefas.
/// </summary>
public sealed class TaskModelConfiguration : IEntityTypeConfiguration<TaskModel>
{
    /// <summary>
    /// Configura os campos e relacionamentos da entidade.
    /// </summary>
    /// <param name="builder">Construtor utilizado para configurar a entidade.</param>
    public void Configure(EntityTypeBuilder<TaskModel> builder)
    {
        builder.Property(item => item.Title).HasMaxLength(160).IsRequired();

        builder.HasIndex(item => new
        {
            item.UserId,
            item.IsCompleted,
            item.DueDate,
        });

        builder
            .HasOne(item => item.User)
            .WithMany(user => user.Tasks)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
