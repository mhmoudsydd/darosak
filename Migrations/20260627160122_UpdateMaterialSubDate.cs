using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace darsakApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMaterialSubDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "SubscribedAt",
                table: "MaterialSubscriptions",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SubscribedAt",
                table: "MaterialSubscriptions",
                type: "datetime(6)",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");
        }
    }
}
