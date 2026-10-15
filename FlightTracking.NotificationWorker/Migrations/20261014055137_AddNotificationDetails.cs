using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightTracking.NotificationWorker.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BaroAltitude",
                table: "NotificationHistories",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Callsign",
                table: "NotificationHistories",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "NotificationHistories",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "DB")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Icao24",
                table: "NotificationHistories",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "NotificationHistories",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "NotificationHistories",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "NotificationHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ZoneName",
                table: "NotificationHistories",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaroAltitude",
                table: "NotificationHistories");

            migrationBuilder.DropColumn(
                name: "Callsign",
                table: "NotificationHistories");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "NotificationHistories");

            migrationBuilder.DropColumn(
                name: "Icao24",
                table: "NotificationHistories");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "NotificationHistories");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "NotificationHistories");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "NotificationHistories");

            migrationBuilder.DropColumn(
                name: "ZoneName",
                table: "NotificationHistories");
        }
    }
}
