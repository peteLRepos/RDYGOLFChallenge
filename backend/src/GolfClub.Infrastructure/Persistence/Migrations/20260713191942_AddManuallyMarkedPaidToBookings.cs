using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfClub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManuallyMarkedPaidToBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ManuallyMarkedPaid",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManuallyMarkedPaid",
                table: "Bookings");
        }
    }
}
