using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarEvents.Migrations
{
    /// <inheritdoc />
    public partial class FinalModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_PaymentId",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PaymentId",
                table: "Bookings",
                column: "PaymentId",
                unique: true,
                filter: "[PaymentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_PaymentId",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PaymentId",
                table: "Bookings",
                column: "PaymentId");
        }
    }
}
