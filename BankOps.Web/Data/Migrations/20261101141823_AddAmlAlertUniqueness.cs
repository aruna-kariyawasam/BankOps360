using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankOps.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAmlAlertUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AmlAlerts_BankTransactionId",
                table: "AmlAlerts");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewedByUserId",
                table: "AmlAlerts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AmlAlerts_BankTransactionId_RuleCode",
                table: "AmlAlerts",
                columns: new[] { "BankTransactionId", "RuleCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AmlAlerts_BankTransactionId_RuleCode",
                table: "AmlAlerts");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewedByUserId",
                table: "AmlAlerts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AmlAlerts_BankTransactionId",
                table: "AmlAlerts",
                column: "BankTransactionId");
        }
    }
}
