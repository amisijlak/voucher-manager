using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoucherMgt.DAL;

#nullable disable

namespace VoucherMgt.DAL.Migrations
{
    [DbContext(typeof(VoucherMgtDbContext))]
    [Migration("20261004170000_AccountabilityItemProof")]
    public class AccountabilityItemProof : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                schema: "voucher",
                table: "AccountabilityLines",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "SpentOn",
                schema: "voucher",
                table: "AccountabilityLines",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "SpentOn",
                schema: "voucher",
                table: "AccountabilityLines",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Comment",
                schema: "voucher",
                table: "AccountabilityLines");
        }
    }
}
