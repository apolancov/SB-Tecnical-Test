using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestEvidenceAndClosedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "Requests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceUrl",
                table: "Requests",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ClosedAt",
                table: "Requests",
                column: "ClosedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_DueDate",
                table: "Requests",
                column: "DueDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_DueDate",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_ClosedAt",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "EvidenceUrl",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Requests");
        }
    }
}