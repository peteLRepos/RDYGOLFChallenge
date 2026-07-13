using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfClub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingOwnershipAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bookings created under the old schema have no real BookerId/PaymentMethod to backfill
            // (there's no "system user" to assign them to, and no way to know how they'd have been
            // paid) — they're incompatible with the new ownership/payment model, not just
            // differently-shaped. Clearing them here is safe: this is pre-production dev/seed data
            // only, a fresh checkout starts with an empty table anyway, and leaving them in place
            // would make the AddForeignKey below fail outright (BookerId's backfilled default of
            // Guid.Empty never matches a real User row).
            migrationBuilder.Sql("DELETE FROM \"Bookings\";");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Bookings");

            migrationBuilder.AddColumn<Guid>(
                name: "BookerId",
                table: "Bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookerId",
                table: "Bookings",
                column: "BookerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_BookerId",
                table: "Bookings",
                column: "BookerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_BookerId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BookerId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookerId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Bookings");

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Bookings",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Bookings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
