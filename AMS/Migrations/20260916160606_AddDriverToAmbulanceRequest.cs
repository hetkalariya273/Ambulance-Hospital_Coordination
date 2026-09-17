using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverToAmbulanceRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DriverId",
                table: "AmbulanceRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AmbulanceRequests_DriverId",
                table: "AmbulanceRequests",
                column: "DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_AmbulanceRequests_Drivers_DriverId",
                table: "AmbulanceRequests",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "DriverId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AmbulanceRequests_Drivers_DriverId",
                table: "AmbulanceRequests");

            migrationBuilder.DropIndex(
                name: "IX_AmbulanceRequests_DriverId",
                table: "AmbulanceRequests");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "AmbulanceRequests");
        }
    }
}
