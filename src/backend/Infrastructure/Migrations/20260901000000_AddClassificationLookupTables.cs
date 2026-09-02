using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClassificationLookupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateTable(
                name: "StatePowers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatePowers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_StatePowers_Name",
                table: "StatePowers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateTable(
                name: "Sectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sectors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Sectors_Name",
                table: "Sectors",
                column: "Name",
                unique: true);

            migrationBuilder.Sql(@"
                INSERT INTO dbo.Categories (Id, Name)
                SELECT NEWID(), Category
                FROM (SELECT DISTINCT Category FROM dbo.Institutions) AS source;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO dbo.StatePowers (Id, Name)
                SELECT NEWID(), StatePower
                FROM (SELECT DISTINCT StatePower FROM dbo.Institutions) AS source;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO dbo.Sectors (Id, Name)
                SELECT NEWID(), Sector
                FROM (SELECT DISTINCT Sector FROM dbo.Institutions) AS source;
            ");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Institutions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StatePowerId",
                table: "Institutions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SectorId",
                table: "Institutions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE i SET i.CategoryId = c.Id FROM dbo.Institutions AS i INNER JOIN dbo.Categories AS c ON c.Name = i.Category;");

            migrationBuilder.Sql(
                "UPDATE i SET i.StatePowerId = s.Id FROM dbo.Institutions AS i INNER JOIN dbo.StatePowers AS s ON s.Name = i.StatePower;");

            migrationBuilder.Sql(
                "UPDATE i SET i.SectorId = s.Id FROM dbo.Institutions AS i INNER JOIN dbo.Sectors AS s ON s.Name = i.Sector;");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Institutions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StatePowerId",
                table: "Institutions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SectorId",
                table: "Institutions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Institutions_Category",
                table: "Institutions");

            migrationBuilder.DropIndex(
                name: "IX_Institutions_Sector",
                table: "Institutions");

            migrationBuilder.DropIndex(
                name: "IX_Institutions_StatePower",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "StatePower",
                table: "Institutions");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_CategoryId",
                table: "Institutions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_SectorId",
                table: "Institutions",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_StatePowerId",
                table: "Institutions",
                column: "StatePowerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Institutions_Categories_CategoryId",
                table: "Institutions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Institutions_Sectors_SectorId",
                table: "Institutions",
                column: "SectorId",
                principalTable: "Sectors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Institutions_StatePowers_StatePowerId",
                table: "Institutions",
                column: "StatePowerId",
                principalTable: "StatePowers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Institutions_Categories_CategoryId",
                table: "Institutions");

            migrationBuilder.DropForeignKey(
                name: "FK_Institutions_Sectors_SectorId",
                table: "Institutions");

            migrationBuilder.DropForeignKey(
                name: "FK_Institutions_StatePowers_StatePowerId",
                table: "Institutions");

            migrationBuilder.DropIndex(
                name: "IX_Institutions_CategoryId",
                table: "Institutions");

            migrationBuilder.DropIndex(
                name: "IX_Institutions_SectorId",
                table: "Institutions");

            migrationBuilder.DropIndex(
                name: "IX_Institutions_StatePowerId",
                table: "Institutions");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Institutions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "Institutions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "StatePower",
                table: "Institutions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false);

            migrationBuilder.Sql(
                "UPDATE i SET i.Category = c.Name FROM dbo.Institutions AS i INNER JOIN dbo.Categories AS c ON c.Id = i.CategoryId;");

            migrationBuilder.Sql(
                "UPDATE i SET i.StatePower = s.Name FROM dbo.Institutions AS i INNER JOIN dbo.StatePowers AS s ON s.Id = i.StatePowerId;");

            migrationBuilder.Sql(
                "UPDATE i SET i.Sector = s.Name FROM dbo.Institutions AS i INNER JOIN dbo.Sectors AS s ON s.Id = i.SectorId;");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "SectorId",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "StatePowerId",
                table: "Institutions");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_Category",
                table: "Institutions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_Sector",
                table: "Institutions",
                column: "Sector");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_StatePower",
                table: "Institutions",
                column: "StatePower");

            migrationBuilder.DropTable(
                name: "Sectors");

            migrationBuilder.DropTable(
                name: "StatePowers");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}