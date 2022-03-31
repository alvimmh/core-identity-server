using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreIdentityServer.Internals.Migrations
{
    public partial class CreateUserAccessRecordsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserAccessRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    AccessorId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccessRecords", x => x.Id);

                    table.ForeignKey(
                        name: "FK_UserAccessRecords_AspNetUsers_AccessorId",
                        column: x => x.AccessorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id"
                    );

                    table.ForeignKey(
                        name: "FK_UserAccessRecords_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessRecords_AccessorId",
                table: "UserAccessRecords",
                column: "AccessorId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessRecords_UserId",
                table: "UserAccessRecords",
                column: "UserId"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAccessRecords"
            );
        }
    }
}
