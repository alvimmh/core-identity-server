using Microsoft.EntityFrameworkCore.Migrations;

namespace CoreIdentityServer.Internals.Migrations
{
    public partial class AddRequiresAuthenticatorResetFieldToAspNetUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresAuthenticatorReset",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: false
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresAuthenticatorReset",
                table: "AspNetUsers"
            );
        }
    }
}
