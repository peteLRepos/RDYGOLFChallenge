using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GolfClub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHandicapAndBookingPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayerCount",
                table: "Bookings");

            // Backfills to 56 (the same "no handicap established yet" default User's own
            // constructor uses), not 0 — 0 would understate a legacy user's handicap rather than
            // erring toward the safer, worse-case default the domain otherwise applies.
            migrationBuilder.AddColumn<int>(
                name: "Handicap",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 56);

            migrationBuilder.CreateTable(
                name: "BookingPlayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Handicap = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingPlayers_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingPlayers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingPlayers_BookingId_UserId",
                table: "BookingPlayers",
                columns: new[] { "BookingId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingPlayers_UserId",
                table: "BookingPlayers",
                column: "UserId");

            // Backfill: every pre-existing Booking becomes its own booker's first (and only)
            // player, same "grandfather the old shape in as valid new-shape data" approach as the
            // BookerId backfill in the original booking-lifecycle migration. Handicap defaults to
            // 56 (max/worst) for the same reason as the Users.Handicap backfill above — the
            // combined-handicap cap should never be silently under-reported for legacy data.
            migrationBuilder.Sql(
                """
                INSERT INTO "BookingPlayers" ("Id", "UserId", "Handicap", "JoinedAt", "BookingId")
                SELECT gen_random_uuid(), "BookerId", 56, "CreatedAt", "Id" FROM "Bookings";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingPlayers");

            migrationBuilder.DropColumn(
                name: "Handicap",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "PlayerCount",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
