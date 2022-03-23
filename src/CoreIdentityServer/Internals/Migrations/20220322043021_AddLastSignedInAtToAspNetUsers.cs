using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreIdentityServer.Internals.Migrations
{
    public partial class AddLastSignedInAtToAspNetUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignInTimeStamps",
                table: "AspNetUsers"
            );

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSignedInAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true,
                defaultValue: null
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSignedInAt",
                table: "AspNetUsers"
            );

            migrationBuilder.AddColumn<string>(
                name: "SignInTimeStamps",
                table: "AspNetUsers",
                type: "text",
                nullable: true
            );
        }
    }
}
