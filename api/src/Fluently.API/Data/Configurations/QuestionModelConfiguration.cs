using Fluently.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluently.API.Data.Configurations;

/// <summary>
/// Mapeamento das questões no banco de dados.
/// </summary>
public sealed class QuestionModelConfiguration : IEntityTypeConfiguration<QuestionModel>
{
    /// <summary>
    /// Define as propriedades, os índices e os relacionamentos da questão.
    /// </summary>
    /// <param name="builder">Construtor utilizado para configurar a entidade.</param>
    public void Configure(EntityTypeBuilder<QuestionModel> builder)
    {
        builder.HasKey(question => question.Id);
        builder.HasIndex(question => question.UserId).IsUnique().HasFilter("\"AnsweredAt\" IS NULL");
        builder.HasIndex(question => new { question.UserId, question.ContextFingerprint }).IsUnique();
        builder.HasIndex(question => new { question.UserId, question.CreatedAt });

        builder.Property(question => question.Context).HasMaxLength(500).IsRequired();
        builder.Property(question => question.Question).HasMaxLength(500).IsRequired();
        builder
            .Property(question => question.QuestionTranslation)
            .HasMaxLength(500)
            .IsRequired()
            .HasComment("Tradução para português da frase apresentada na questão.");
        builder.Property(question => question.ContextFingerprint).HasMaxLength(64).IsRequired();
        builder.Property(question => question.Alternatives).HasColumnType("text[]").IsRequired();
        builder
            .Property(question => question.AlternativeTranslations)
            .HasColumnType("text[]")
            .IsRequired()
            .HasComment("Traduções em português das cinco alternativas, na mesma ordem das alternativas.");
        builder
            .Property(question => question.CorrectAlternativeIndex)
            .IsRequired()
            .HasComment("Índice de 1 a 5 da alternativa correta.");
        builder
            .Property(question => question.SubmittedAlternativeIndex)
            .HasComment("Índice de 1 a 5 da alternativa selecionada pelo usuário.");
        builder.Property(question => question.BaseXp).HasDefaultValue(15).IsRequired();

        builder
            .HasOne(question => question.User)
            .WithMany(user => user.Questions)
            .HasForeignKey(question => question.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Questions_BaseXp", "\"BaseXp\" > 0");
            table.HasCheckConstraint(
                "CK_Questions_CorrectAlternativeIndex",
                "\"CorrectAlternativeIndex\" BETWEEN 1 AND 5"
            );
            table.HasCheckConstraint(
                "CK_Questions_SubmittedAlternativeIndex",
                "\"SubmittedAlternativeIndex\" IS NULL OR \"SubmittedAlternativeIndex\" BETWEEN 1 AND 5"
            );
            table.HasCheckConstraint("CK_Questions_AwardedXp", "\"AwardedXp\" IS NULL OR \"AwardedXp\" >= 0");
            table.HasCheckConstraint(
                "CK_Questions_StreakAfterAnswer",
                "\"StreakAfterAnswer\" IS NULL OR \"StreakAfterAnswer\" >= 0"
            );
        });
    }
}
