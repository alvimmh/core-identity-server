using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreIdentityServer.Internals.Migrations
{
    public partial class RemoveForeignKeyConstraintsFromUserAccessRecords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAccessRecords_AspNetUsers_AccessorId",
                table: "UserAccessRecords"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_UserAccessRecords_AspNetUsers_UserId",
                table: "UserAccessRecords"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_UserAccessRecords_AspNetUsers_AccessorId",
                table: "UserAccessRecords",
                column: "AccessorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccessRecords_AspNetUsers_UserId",
                table: "UserAccessRecords",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id"
            );
        }
    }
}
