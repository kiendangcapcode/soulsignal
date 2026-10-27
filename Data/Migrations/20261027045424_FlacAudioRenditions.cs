using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoulSignal.Data.Migrations
{
    /// <inheritdoc />
    public partial class FlacAudioRenditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HighQualityFileName",
                table: "Songs",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HighQualityFileName",
                table: "Songs");
        }
    }
}
