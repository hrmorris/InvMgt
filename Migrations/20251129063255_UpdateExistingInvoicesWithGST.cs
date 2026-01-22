using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceManagement.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExistingInvoicesWithGST : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing invoices: current TotalAmount becomes SubTotal, calculate GST, update TotalAmount
            migrationBuilder.Sql(@"
                UPDATE Invoices 
                SET SubTotal = TotalAmount,
                    GSTAmount = ROUND(TotalAmount * 0.10, 2),
                    TotalAmount = ROUND(TotalAmount + (TotalAmount * 0.10), 2),
                    GSTRate = 10.0
                WHERE SubTotal = 0 OR SubTotal = '0.0';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: Remove GST from TotalAmount, clear SubTotal and GSTAmount
            migrationBuilder.Sql(@"
                UPDATE Invoices 
                SET TotalAmount = SubTotal,
                    SubTotal = 0,
                    GSTAmount = 0,
                    GSTRate = 0
                WHERE GSTRate = 10.0;
            ");
        }
    }
}
