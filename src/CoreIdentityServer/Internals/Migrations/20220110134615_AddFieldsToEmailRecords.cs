using Microsoft.EntityFrameworkCore.Migrations;

namespace CoreIdentityServer.Internals.Migrations
{
    public partial class AddFieldsToEmailRecords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Archived",
                table: "EmailRecords",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<int>(
                name: "SendAttempts",
                table: "EmailRecords",
                nullable: false,
                defaultValue: 0
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Archived",
                table: "EmailRecords"
            );

            migrationBuilder.DropColumn(
                name: "SendAttempts",
                table: "EmailRecords"
            );
        }
    }
}
