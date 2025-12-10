using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CBRF.DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "currency_rates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    NumCode = table.Column<int>(type: "integer", nullable: false),
                    CharCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    Nominal = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    VunitRate = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currency_rates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_currency_rates_CharCode",
                table: "currency_rates",
                column: "CharCode");

            migrationBuilder.CreateIndex(
                name: "IX_currency_rates_Date",
                table: "currency_rates",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "currency_rates");
        }
    }
}
