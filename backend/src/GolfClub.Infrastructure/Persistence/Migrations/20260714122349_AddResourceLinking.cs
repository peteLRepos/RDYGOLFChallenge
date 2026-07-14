using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfClub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LinkedResourceId",
                table: "Resources",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resources_LinkedResourceId",
                table: "Resources",
                column: "LinkedResourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Resources_LinkedResourceId",
                table: "Resources",
                column: "LinkedResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Resources_LinkedResourceId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_LinkedResourceId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "LinkedResourceId",
                table: "Resources");
        }
    }
}
