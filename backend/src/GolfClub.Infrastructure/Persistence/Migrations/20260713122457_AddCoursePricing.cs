using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfClub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCoursePricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PricePerPlayer",
                table: "Resources",
                type: "numeric(10,2)",
                nullable: true);

            // Backfills to 1, not 0 — Booking's own constructor requires PlayerCount >= 1, so 0
            // would leave any pre-existing row violating an invariant the domain otherwise
            // guarantees everywhere (EF loads existing rows through the parameterless private
            // constructor, bypassing that check on read).
            migrationBuilder.AddColumn<int>(
                name: "PlayerCount",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                table: "Bookings",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricePerPlayer",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "PlayerCount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TotalPrice",
                table: "Bookings");
        }
    }
}
