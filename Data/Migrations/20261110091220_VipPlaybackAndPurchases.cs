using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoulSignal.Data.Migrations
{
    /// <inheritdoc />
    public partial class VipPlaybackAndPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DemoOrders_UserId",
                table: "DemoOrders");

            migrationBuilder.AddColumn<bool>(
                name: "AllowDownload",
                table: "Songs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "DemoOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemoOrders_UserId_RequestId",
                table: "DemoOrders",
                columns: new[] { "UserId", "RequestId" },
                unique: true,
                filter: "[RequestId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DemoOrders_UserId_RequestId",
                table: "DemoOrders");

            migrationBuilder.DropColumn(
                name: "AllowDownload",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "DemoOrders");

            migrationBuilder.CreateIndex(
                name: "IX_DemoOrders_UserId",
                table: "DemoOrders",
                column: "UserId");
        }
    }
}
