using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoucherMgt.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AccountabilitySourceLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequisitionLineId",
                schema: "voucher",
                table: "AccountabilityLines",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountabilityLines_RequisitionLineId",
                schema: "voucher",
                table: "AccountabilityLines",
                column: "RequisitionLineId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountabilityLines_RequisitionLines_RequisitionLineId",
                schema: "voucher",
                table: "AccountabilityLines",
                column: "RequisitionLineId",
                principalSchema: "voucher",
                principalTable: "RequisitionLines",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountabilityLines_RequisitionLines_RequisitionLineId",
                schema: "voucher",
                table: "AccountabilityLines");

            migrationBuilder.DropIndex(
                name: "IX_AccountabilityLines_RequisitionLineId",
                schema: "voucher",
                table: "AccountabilityLines");

            migrationBuilder.DropColumn(
                name: "RequisitionLineId",
                schema: "voucher",
                table: "AccountabilityLines");
        }
    }
}
