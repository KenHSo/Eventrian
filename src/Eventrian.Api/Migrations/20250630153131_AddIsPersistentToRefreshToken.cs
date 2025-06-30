﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventrian.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPersistentToRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPersistent",
                table: "RefreshTokens",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPersistent",
                table: "RefreshTokens");
        }
    }
}
