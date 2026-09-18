using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fluently.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NormalizedEmail = table.Column<string>(
                        type: "character varying(320)",
                        maxLength: 320,
                        nullable: false
                    ),
                    PasswordHash = table.Column<string>(
                        type: "character varying(1024)",
                        maxLength: 1024,
                        nullable: false
                    ),
                    TotalXp = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CurrentStreak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Proficiency = table.Column<int>(
                        type: "integer",
                        nullable: true,
                        comment: "1 = A1; 2 = A2; 3 = B1; 4 = B2; 5 = C1; 6 = C2"
                    ),
                    Bio = table.Column<string>(
                        type: "character varying(2000)",
                        maxLength: 2000,
                        nullable: true,
                        comment: "Biografia utilizada como contexto para gerar questões personalizadas."
                    ),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_CurrentStreak", "\"CurrentStreak\" >= 0");
                    table.CheckConstraint("CK_Users_TotalXp", "\"TotalXp\" >= 0");
                }
            );

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Context = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Question = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    QuestionTranslation = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false,
                        comment: "Tradução para português da frase apresentada na questão."
                    ),
                    ContextFingerprint = table.Column<string>(
                        type: "character varying(64)",
                        maxLength: 64,
                        nullable: false
                    ),
                    Alternatives = table.Column<string[]>(type: "text[]", nullable: false),
                    AlternativeTranslations = table.Column<string[]>(
                        type: "text[]",
                        nullable: false,
                        comment: "Traduções em português das cinco alternativas, na mesma ordem das alternativas."
                    ),
                    CorrectAlternativeIndex = table.Column<int>(
                        type: "integer",
                        nullable: false,
                        comment: "Índice de 1 a 5 da alternativa correta."
                    ),
                    BaseXp = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    SubmittedAlternativeIndex = table.Column<int>(
                        type: "integer",
                        nullable: true,
                        comment: "Índice de 1 a 5 da alternativa selecionada pelo usuário."
                    ),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: true),
                    AwardedXp = table.Column<int>(type: "integer", nullable: true),
                    StreakAfterAnswer = table.Column<int>(type: "integer", nullable: true),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.CheckConstraint("CK_Questions_AwardedXp", "\"AwardedXp\" IS NULL OR \"AwardedXp\" >= 0");
                    table.CheckConstraint("CK_Questions_BaseXp", "\"BaseXp\" > 0");
                    table.CheckConstraint(
                        "CK_Questions_CorrectAlternativeIndex",
                        "\"CorrectAlternativeIndex\" BETWEEN 1 AND 5"
                    );
                    table.CheckConstraint(
                        "CK_Questions_StreakAfterAnswer",
                        "\"StreakAfterAnswer\" IS NULL OR \"StreakAfterAnswer\" >= 0"
                    );
                    table.CheckConstraint(
                        "CK_Questions_SubmittedAlternativeIndex",
                        "\"SubmittedAlternativeIndex\" IS NULL OR \"SubmittedAlternativeIndex\" BETWEEN 1 AND 5"
                    );
                    table.ForeignKey(
                        name: "FK_Questions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Questions_UserId",
                table: "Questions",
                column: "UserId",
                unique: true,
                filter: "\"AnsweredAt\" IS NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Questions_UserId_ContextFingerprint",
                table: "Questions",
                columns: new[] { "UserId", "ContextFingerprint" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Questions_UserId_CreatedAt",
                table: "Questions",
                columns: new[] { "UserId", "CreatedAt" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Users_TotalXp_CreatedAt",
                table: "Users",
                columns: new[] { "TotalXp", "CreatedAt" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Questions");

            migrationBuilder.DropTable(name: "Users");
        }
    }
}
