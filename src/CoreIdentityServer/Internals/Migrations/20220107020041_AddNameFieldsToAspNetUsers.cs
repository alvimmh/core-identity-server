using Microsoft.EntityFrameworkCore.Migrations;

namespace CoreIdentityServer.Internals.Migrations
{
    public partial class AddNameFieldsToAspNetUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                nullable: false
            );

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                nullable: false
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers"
            );

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "AspNetUsers"
            );
        }
    }
}
