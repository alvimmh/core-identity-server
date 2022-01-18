using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreIdentityServer.Internals.Migrations
{
    public partial class UpdateEmailRecordDateTimeColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "EmailRecords",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAt",
                table: "EmailRecords",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "EmailRecords",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "EmailRecords",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAt",
                table: "EmailRecords",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "EmailRecords",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone"
            );
        }
    }
}
