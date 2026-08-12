using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpellingBee.Words.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWordAudioKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioKey",
                table: "Words");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioKey",
                table: "Words",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }
    }
}
