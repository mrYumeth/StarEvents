using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarEvents.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupportPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxTicketsPerBooking = table.Column<int>(type: "int", nullable: false),
                    BookingCancellationHours = table.Column<int>(type: "int", nullable: false),
                    EnableQRCodeTickets = table.Column<bool>(type: "bit", nullable: false),
                    AcceptCreditCards = table.Column<bool>(type: "bit", nullable: false),
                    AcceptPayPal = table.Column<bool>(type: "bit", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnableLoyaltyProgram = table.Column<bool>(type: "bit", nullable: false),
                    PointsPer100LKR = table.Column<int>(type: "int", nullable: false),
                    PointsExpiryDays = table.Column<int>(type: "int", nullable: false),
                    EmailOnBookingConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    EmailOnEventReminder = table.Column<bool>(type: "bit", nullable: false),
                    EmailForPromotions = table.Column<bool>(type: "bit", nullable: false),
                    RequireEmailVerification = table.Column<bool>(type: "bit", nullable: false),
                    SessionTimeoutMinutes = table.Column<int>(type: "int", nullable: false),
                    PasswordMinLength = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
