using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalHub.Migrations
{
    /// <inheritdoc />
    public partial class AddKycUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "KycVerifications",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "KycVerifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "KycVerifications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_PropertyId",
                table: "Wishlists",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_KycVerifications_UserId",
                table: "KycVerifications",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_KycVerifications_AspNetUsers_UserId",
                table: "KycVerifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Wishlists_Properties_PropertyId",
                table: "Wishlists",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KycVerifications_AspNetUsers_UserId",
                table: "KycVerifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Wishlists_Properties_PropertyId",
                table: "Wishlists");

            migrationBuilder.DropIndex(
                name: "IX_Wishlists_PropertyId",
                table: "Wishlists");

            migrationBuilder.DropIndex(
                name: "IX_KycVerifications_UserId",
                table: "KycVerifications");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "KycVerifications");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "KycVerifications");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "KycVerifications");
        }
    }
}
