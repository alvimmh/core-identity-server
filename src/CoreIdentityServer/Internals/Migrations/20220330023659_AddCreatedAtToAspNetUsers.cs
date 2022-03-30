using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreIdentityServer.Internals.Migrations
{
    public partial class AddCreatedAtToAspNetUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: false
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUsers"
            );
        }
    }
}
