using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventrian.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUsedAtToRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UsedAt",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsedAt",
                table: "RefreshTokens");
        }
    }
}
