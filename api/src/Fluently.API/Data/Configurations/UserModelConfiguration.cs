using Fluently.API.Enums;
using Fluently.API.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluently.API.Data.Configurations;

/// <summary>
/// Mapeamento dos usuários no banco de dados.
/// </summary>
public sealed class UserModelConfiguration : IEntityTypeConfiguration<UserModel>
{
    /// <summary>
    /// Define as propriedades, os índices e as restrições do usuário.
    /// </summary>
    /// <param name="builder">Construtor utilizado para configurar a entidade.</param>
    public void Configure(EntityTypeBuilder<UserModel> builder)
    {
        builder.HasKey(user => user.Id);
        builder.HasIndex(user => user.NormalizedEmail).IsUnique();
        builder.HasIndex(user => new { user.TotalXp, user.CreatedAt });

        builder.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.LastName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
        builder.Property(user => user.NormalizedEmail).HasMaxLength(320).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(1024).IsRequired();
        builder.Property(user => user.TotalXp).HasDefaultValue(0L).IsRequired();
        builder.Property(user => user.CurrentStreak).HasDefaultValue(0).IsRequired();
        builder.Property(user => user.Proficiency)
            .HasColumnType("integer")
            .HasComment("1 = A1; 2 = A2; 3 = B1; 4 = B2; 5 = C1; 6 = C2");
        builder.Property(user => user.Bio)
            .HasMaxLength(2000)
            .HasComment("Biografia utilizada como contexto para gerar questões personalizadas.");
        builder.Property(user => user.ProfileImageBase64)
            .HasColumnType("text");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Users_TotalXp", "\"TotalXp\" >= 0");
            table.HasCheckConstraint("CK_Users_CurrentStreak", "\"CurrentStreak\" >= 0");
        });
    }
}
