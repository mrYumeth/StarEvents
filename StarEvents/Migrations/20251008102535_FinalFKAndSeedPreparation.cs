using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarEvents.Migrations
{
    /// <inheritdoc />
    public partial class FinalFKAndSeedPreparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Removed redundant DropForeignKey, DropIndex, and DropColumn for OrganizerId1
            // as they were successfully executed in the preceding migration '20251007132101_ChangeOrganizerIdToString'.

            // NOTE: Removed redundant CreateIndex and AddForeignKey calls below, 
            // as the previous migration (20251007132101_ChangeOrganizerIdToString) already created them.

            migrationBuilder.AlterColumn<string>(
                name: "OrganizerId",
                table: "Events",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            // migrationBuilder.CreateIndex(...) REMOVED: Index already exists.

            // migrationBuilder.AddForeignKey(...) REMOVED: Foreign Key already exists.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_AspNetUsers_OrganizerId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_OrganizerId",
                table: "Events");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizerId",
                table: "Events",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "OrganizerId1",
                table: "Events",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganizerId1",
                table: "Events",
                column: "OrganizerId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_AspNetUsers_OrganizerId1",
                table: "Events",
                column: "OrganizerId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
