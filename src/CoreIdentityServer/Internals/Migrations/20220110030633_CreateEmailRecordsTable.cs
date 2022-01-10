using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CoreIdentityServer.Internals.Migrations
{
    public partial class CreateEmailRecordsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailRecords",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    SentFrom = table.Column<string>(nullable: false),
                    SentTo = table.Column<string>(nullable: false),
                    Subject = table.Column<string>(nullable: false),
                    Body = table.Column<string>(nullable: false),
                    SentAt = table.Column<DateTime>(nullable: true),
                    ResentAt = table.Column<string>(nullable: true),
                    CancelledAt = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    UpdatedAt = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailRecords", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailRecords");
        }
    }
}
