using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchPaymentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BatchPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BatchReference = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BatchName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ScheduledPaymentDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProcessedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BankAccount = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ApprovedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchPayments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BatchPaymentItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BatchPaymentId = table.Column<int>(type: "INTEGER", nullable: false),
                    InvoiceId = table.Column<int>(type: "INTEGER", nullable: false),
                    AmountToPay = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    IsProcessed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PaymentId = table.Column<int>(type: "INTEGER", nullable: true),
                    AddedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchPaymentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchPaymentItems_BatchPayments_BatchPaymentId",
                        column: x => x.BatchPaymentId,
                        principalTable: "BatchPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchPaymentItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BatchPaymentItems_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BatchPaymentItems_BatchPaymentId_InvoiceId",
                table: "BatchPaymentItems",
                columns: new[] { "BatchPaymentId", "InvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BatchPaymentItems_InvoiceId",
                table: "BatchPaymentItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchPaymentItems_PaymentId",
                table: "BatchPaymentItems",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchPayments_BatchReference",
                table: "BatchPayments",
                column: "BatchReference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BatchPaymentItems");

            migrationBuilder.DropTable(
                name: "BatchPayments");
        }
    }
}
