using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfClub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingPlayerPaymentAndOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AddedByUserId",
                table: "BookingPlayers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "BookingPlayers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Backfill from the booking each row belongs to, before Bookings.PaymentMethod is
            // dropped below — every pre-existing BookingPlayers row is the sole/backfilled
            // booker-player (see the handicap migration), so it inherits the payment method that
            // used to live on Bookings, and self-references AddedByUserId since that player added
            // themselves (there was no invite/self-join concept before this feature).
            migrationBuilder.Sql(@"
                UPDATE ""BookingPlayers"" bp
                SET ""PaymentMethod"" = b.""PaymentMethod"",
                    ""AddedByUserId"" = bp.""UserId""
                FROM ""Bookings"" b
                WHERE bp.""BookingId"" = b.""Id"";
            ");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Cash");

            // Best-effort: a booking may have players with differing payment methods by the time
            // this runs, which the pre-feature schema couldn't represent — falls back to the
            // booker's own payment method, matching how the column originally represented "the
            // booker's payment method" before per-player payment existed.
            migrationBuilder.Sql(@"
                UPDATE ""Bookings"" b
                SET ""PaymentMethod"" = bp.""PaymentMethod""
                FROM ""BookingPlayers"" bp
                WHERE bp.""BookingId"" = b.""Id""
                  AND bp.""UserId"" = b.""BookerId"";
            ");

            migrationBuilder.DropColumn(
                name: "AddedByUserId",
                table: "BookingPlayers");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "BookingPlayers");
        }
    }
}
