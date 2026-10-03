using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class updated_driver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AmbulanceRequests_AspNetUsers_UserId",
                table: "AmbulanceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AmbulanceRequests_Drivers_DriverId",
                table: "AmbulanceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Ambulances_Drivers_DriverId",
                table: "Ambulances");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Drivers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AmbulanceRequests",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_UserId",
                table: "Drivers",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AmbulanceRequests_AspNetUsers_UserId",
                table: "AmbulanceRequests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AmbulanceRequests_Drivers_DriverId",
                table: "AmbulanceRequests",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "DriverId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Ambulances_Drivers_DriverId",
                table: "Ambulances",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "DriverId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_AspNetUsers_UserId",
                table: "Drivers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AmbulanceRequests_AspNetUsers_UserId",
                table: "AmbulanceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AmbulanceRequests_Drivers_DriverId",
                table: "AmbulanceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Ambulances_Drivers_DriverId",
                table: "Ambulances");

            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_AspNetUsers_UserId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_UserId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Drivers");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AmbulanceRequests",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_AmbulanceRequests_AspNetUsers_UserId",
                table: "AmbulanceRequests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AmbulanceRequests_Drivers_DriverId",
                table: "AmbulanceRequests",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "DriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ambulances_Drivers_DriverId",
                table: "Ambulances",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "DriverId");
        }
    }
}
