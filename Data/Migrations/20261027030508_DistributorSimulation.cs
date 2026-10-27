using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoulSignal.Data.Migrations
{
    /// <inheritdoc />
    public partial class DistributorSimulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverFileName",
                table: "Songs",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "Songs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "Songs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedById",
                table: "Songs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RightsEvidence",
                table: "Songs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RightsHolder",
                table: "Songs",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PublisherApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrganizationName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RepresentativeName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Introduction = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    RightsStatement = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublisherApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublisherApplications_AspNetUsers_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublisherApplications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Songs_ReviewedById",
                table: "Songs",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_PublisherApplications_ReviewedById",
                table: "PublisherApplications",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_PublisherApplications_UserId",
                table: "PublisherApplications",
                column: "UserId",
                unique: true,
                filter: "[Status] = N'Pending'");

            migrationBuilder.AddForeignKey(
                name: "FK_Songs_AspNetUsers_ReviewedById",
                table: "Songs",
                column: "ReviewedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Songs_AspNetUsers_ReviewedById",
                table: "Songs");

            migrationBuilder.DropTable(
                name: "PublisherApplications");

            migrationBuilder.DropIndex(
                name: "IX_Songs_ReviewedById",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "CoverFileName",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "ReviewedById",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "RightsEvidence",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "RightsHolder",
                table: "Songs");
        }
    }
}
