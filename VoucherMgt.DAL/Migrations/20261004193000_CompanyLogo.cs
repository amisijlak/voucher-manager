using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoucherMgt.DAL;

#nullable disable

namespace VoucherMgt.DAL.Migrations
{
    [DbContext(typeof(VoucherMgtDbContext))]
    [Migration("20261004193000_CompanyLogo")]
    public class CompanyLogo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Contact",
                schema: "voucher",
                table: "TradingCompanies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoFileName",
                schema: "voucher",
                table: "TradingCompanies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoFilePath",
                schema: "voucher",
                table: "TradingCompanies",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Contact", schema: "voucher", table: "TradingCompanies");
            migrationBuilder.DropColumn(name: "LogoFileName", schema: "voucher", table: "TradingCompanies");
            migrationBuilder.DropColumn(name: "LogoFilePath", schema: "voucher", table: "TradingCompanies");
        }
    }
}
