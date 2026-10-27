using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoulSignal.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkDistributorAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DistributorId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DistributorId",
                table: "AspNetUsers",
                column: "DistributorId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Distributors_DistributorId",
                table: "AspNetUsers",
                column: "DistributorId",
                principalTable: "Distributors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Distributors_DistributorId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DistributorId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DistributorId",
                table: "AspNetUsers");
        }
    }
}
