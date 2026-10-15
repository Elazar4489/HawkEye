using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightTracking.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleAndZoneName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "Users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Operator")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CustomerPriorities",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "CustomerPriorities");
        }
    }
}
