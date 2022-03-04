using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreIdentityServer.Internals.Migrations
{
    public partial class AddSignInTimeStampsToAspNetUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SignInTimeStamps",
                table: "AspNetUsers",
                type: "text",
                nullable: true,
                defaultValue: null
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true,
                defaultValue: null
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignInTimeStamps",
                table: "AspNetUsers"
            );

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AspNetUsers"
            );
        }
    }
}
