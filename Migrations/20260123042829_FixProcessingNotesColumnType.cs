using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceManagement.Migrations
{
    /// <inheritdoc />
    public partial class FixProcessingNotesColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alter ProcessingNotes column to TEXT (no length limit) for PostgreSQL
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'ImportedDocuments' 
                               AND column_name = 'ProcessingNotes') THEN
                        ALTER TABLE ""ImportedDocuments"" 
                        ALTER COLUMN ""ProcessingNotes"" TYPE TEXT;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to varchar(1000) if needed
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'ImportedDocuments' 
                               AND column_name = 'ProcessingNotes') THEN
                        ALTER TABLE ""ImportedDocuments"" 
                        ALTER COLUMN ""ProcessingNotes"" TYPE VARCHAR(1000);
                    END IF;
                END $$;
            ");
        }
    }
}
