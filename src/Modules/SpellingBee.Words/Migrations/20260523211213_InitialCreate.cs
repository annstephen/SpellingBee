using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpellingBee.Words.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Words",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PartOfSpeech = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Definition = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Etymology = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    AudioKey = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AudioFilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ImportedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Words", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Words_Text",
                table: "Words",
                column: "Text",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Words");
        }
    }
}
