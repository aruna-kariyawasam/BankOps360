using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankOps.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneralLedgerAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "BankAccountId",
                table: "TransactionEntries",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "GeneralLedgerAccountId",
                table: "TransactionEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "BankTransactions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "GeneralLedgerAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralLedgerAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionEntries_GeneralLedgerAccountId_CreatedAtUtc",
                table: "TransactionEntries",
                columns: new[] { "GeneralLedgerAccountId", "CreatedAtUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_TransactionEntries_ExactlyOneAccount",
                table: "TransactionEntries",
                sql: "(([BankAccountId] IS NOT NULL AND [GeneralLedgerAccountId] IS NULL) OR ([BankAccountId] IS NULL AND [GeneralLedgerAccountId] IS NOT NULL))");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_IdempotencyKey",
                table: "BankTransactions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneralLedgerAccounts_Code",
                table: "GeneralLedgerAccounts",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionEntries_GeneralLedgerAccounts_GeneralLedgerAccountId",
                table: "TransactionEntries",
                column: "GeneralLedgerAccountId",
                principalTable: "GeneralLedgerAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionEntries_GeneralLedgerAccounts_GeneralLedgerAccountId",
                table: "TransactionEntries");

            migrationBuilder.DropTable(
                name: "GeneralLedgerAccounts");

            migrationBuilder.DropIndex(
                name: "IX_TransactionEntries_GeneralLedgerAccountId_CreatedAtUtc",
                table: "TransactionEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TransactionEntries_ExactlyOneAccount",
                table: "TransactionEntries");

            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_IdempotencyKey",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "GeneralLedgerAccountId",
                table: "TransactionEntries");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "BankTransactions");

            migrationBuilder.AlterColumn<int>(
                name: "BankAccountId",
                table: "TransactionEntries",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
