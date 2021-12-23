using Microsoft.EntityFrameworkCore.Migrations;

namespace CoreIdentityServer.Migrations
{
    public partial class AddAccountRegisteredFieldToAspNetUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AccountRegistered",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountRegistered",
                table: "AspNetUsers");
        }
    }
}
