using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoulSignal.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdminOnlyDistribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Songs_AspNetUsers_PublisherId",
                table: "Songs");

            migrationBuilder.DropTable(
                name: "PublisherApplications");

            migrationBuilder.RenameColumn(
                name: "PublisherId",
                table: "Songs",
                newName: "ImportedById");

            migrationBuilder.RenameIndex(
                name: "IX_Songs_PublisherId",
                table: "Songs",
                newName: "IX_Songs_ImportedById");

            migrationBuilder.AddColumn<int>(
                name: "DistributorId",
                table: "Songs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalRecordingCode",
                table: "Songs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalReleaseCode",
                table: "Songs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Distributors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsSimulation = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Distributors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Songs_DistributorId_ExternalRecordingCode",
                table: "Songs",
                columns: new[] { "DistributorId", "ExternalRecordingCode" },
                unique: true,
                filter: "[DistributorId] IS NOT NULL AND [ExternalRecordingCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_Code",
                table: "Distributors",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Songs_AspNetUsers_ImportedById",
                table: "Songs",
                column: "ImportedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Songs_Distributors_DistributorId",
                table: "Songs",
                column: "DistributorId",
                principalTable: "Distributors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Songs_AspNetUsers_ImportedById",
                table: "Songs");

            migrationBuilder.DropForeignKey(
                name: "FK_Songs_Distributors_DistributorId",
                table: "Songs");

            migrationBuilder.DropTable(
                name: "Distributors");

            migrationBuilder.DropIndex(
                name: "IX_Songs_DistributorId_ExternalRecordingCode",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "DistributorId",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "ExternalRecordingCode",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "ExternalReleaseCode",
                table: "Songs");

            migrationBuilder.RenameColumn(
                name: "ImportedById",
                table: "Songs",
                newName: "PublisherId");

            migrationBuilder.RenameIndex(
                name: "IX_Songs_ImportedById",
                table: "Songs",
                newName: "IX_Songs_PublisherId");

            migrationBuilder.CreateTable(
                name: "PublisherApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReviewedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Introduction = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    OrganizationName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RepresentativeName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RightsStatement = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
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
                name: "FK_Songs_AspNetUsers_PublisherId",
                table: "Songs",
                column: "PublisherId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
