using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfClub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCartToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CartId",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CartId",
                table: "Bookings",
                column: "CartId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Carts_CartId",
                table: "Bookings",
                column: "CartId",
                principalTable: "Carts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Carts_CartId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CartId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CartId",
                table: "Bookings");
        }
    }
}
