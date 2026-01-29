using Microsoft.Data.Sqlite;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Standalone migration tool to copy data from SQLite to PostgreSQL (Supabase)
/// Run with: dotnet run --migrate
/// </summary>
public class DataMigrator
{
    private readonly string _sqliteConnectionString;
    private readonly string _postgresConnectionString;

    public DataMigrator(string sqlitePath, string postgresConnection)
    {
        _sqliteConnectionString = $"Data Source={sqlitePath}";
        _postgresConnectionString = postgresConnection;
    }

    public async Task<string> MigrateAllDataAsync()
    {
        var log = new System.Text.StringBuilder();
        log.AppendLine("Starting data migration from SQLite to PostgreSQL...");
        log.AppendLine($"SQLite: {_sqliteConnectionString}");
        log.AppendLine($"PostgreSQL: {_postgresConnectionString.Substring(0, Math.Min(50, _postgresConnectionString.Length))}...");

        try
        {
            // Migration order matters due to foreign keys
            log.AppendLine(await MigrateTableAsync("Customers"));
            log.AppendLine(await MigrateTableAsync("Suppliers"));
            log.AppendLine(await MigrateTableAsync("SystemSettings"));
            log.AppendLine(await MigrateTableAsync("Roles"));
            log.AppendLine(await MigrateTableAsync("Permissions"));
            log.AppendLine(await MigrateTableAsync("Users"));
            log.AppendLine(await MigrateTableAsync("Requisitions"));
            log.AppendLine(await MigrateTableAsync("RequisitionItems"));
            log.AppendLine(await MigrateTableAsync("PurchaseOrders"));
            log.AppendLine(await MigrateTableAsync("PurchaseOrderItems"));
            log.AppendLine(await MigrateTableAsync("Invoices"));
            log.AppendLine(await MigrateTableAsync("InvoiceItems"));
            log.AppendLine(await MigrateTableAsync("Payments"));
            log.AppendLine(await MigrateTableAsync("PaymentAllocations"));
            log.AppendLine(await MigrateTableAsync("ImportedDocuments"));
            log.AppendLine(await MigrateTableAsync("RolePermissions"));
            log.AppendLine(await MigrateTableAsync("UserRoles"));
            log.AppendLine(await MigrateTableAsync("AuditLogs"));

            // Reset sequences to max ID + 1
            log.AppendLine(await ResetSequencesAsync());

            log.AppendLine("\n✅ Migration completed successfully!");
        }
        catch (Exception ex)
        {
            log.AppendLine($"\n❌ Migration failed: {ex.Message}");
        }

        return log.ToString();
    }

    private async Task<string> MigrateTableAsync(string tableName)
    {
        try
        {
            using var sqliteConn = new SqliteConnection(_sqliteConnectionString);
            await sqliteConn.OpenAsync();

            // Check if table exists in SQLite
            using var checkCmd = new SqliteCommand($"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'", sqliteConn);
            var exists = await checkCmd.ExecuteScalarAsync();
            if (exists == null)
            {
                return $"Migrating {tableName}... SKIPPED (table not found in SQLite)";
            }

            // Get column names from SQLite
            using var schemaCmd = new SqliteCommand($"PRAGMA table_info(\"{tableName}\")", sqliteConn);
            var columns = new List<string>();
            using (var reader = await schemaCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    columns.Add(reader.GetString(1));
                }
            }

            if (columns.Count == 0)
            {
                return $"Migrating {tableName}... SKIPPED (no columns)";
            }

            // Read all data from SQLite
            using var selectCmd = new SqliteCommand($"SELECT * FROM \"{tableName}\"", sqliteConn);
            var rows = new List<object?[]>();
            using (var reader = await selectCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var row = new object?[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    rows.Add(row);
                }
            }

            if (rows.Count == 0)
            {
                return $"Migrating {tableName}... 0 rows";
            }

            // Insert into PostgreSQL
            using var pgConn = new NpgsqlConnection(_postgresConnectionString);
            await pgConn.OpenAsync();

            // Disable triggers temporarily for faster insert
            using var disableTriggers = new NpgsqlCommand($"ALTER TABLE \"{tableName}\" DISABLE TRIGGER ALL", pgConn);
            try { await disableTriggers.ExecuteNonQueryAsync(); } catch { }

            int inserted = 0;
            foreach (var row in rows)
            {
                var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
                var paramList = string.Join(", ", columns.Select((_, i) => $"@p{i}"));
                var insertSql = $"INSERT INTO \"{tableName}\" ({columnList}) VALUES ({paramList}) ON CONFLICT DO NOTHING";

                using var insertCmd = new NpgsqlCommand(insertSql, pgConn);
                for (int i = 0; i < columns.Count; i++)
                {
                    var value = row[i];
                    // Handle SQLite date strings
                    if (value is string strVal && DateTime.TryParse(strVal, out var dateVal))
                    {
                        insertCmd.Parameters.AddWithValue($"@p{i}", dateVal);
                    }
                    else if (value == null)
                    {
                        insertCmd.Parameters.AddWithValue($"@p{i}", DBNull.Value);
                    }
                    else
                    {
                        insertCmd.Parameters.AddWithValue($"@p{i}", value);
                    }
                }

                try
                {
                    await insertCmd.ExecuteNonQueryAsync();
                    inserted++;
                }
                catch (Exception ex)
                {
                    // Log but continue
                    Console.WriteLine($"Row insert warning for {tableName}: {ex.Message}");
                }
            }

            // Re-enable triggers
            using var enableTriggers = new NpgsqlCommand($"ALTER TABLE \"{tableName}\" ENABLE TRIGGER ALL", pgConn);
            try { await enableTriggers.ExecuteNonQueryAsync(); } catch { }

            return $"Migrating {tableName}... {inserted} rows";
        }
        catch (Exception ex)
        {
            return $"Migrating {tableName}... ERROR: {ex.Message}";
        }
    }

    private async Task<string> ResetSequencesAsync()
    {
        var log = new System.Text.StringBuilder();
        log.AppendLine("\nResetting sequences...");

        var tables = new[] { "Customers", "Suppliers", "SystemSettings", "Roles", "Permissions", "Users",
            "Requisitions", "RequisitionItems", "PurchaseOrders", "PurchaseOrderItems",
            "Invoices", "InvoiceItems", "Payments", "PaymentAllocations", "ImportedDocuments",
            "RolePermissions", "UserRoles", "AuditLogs", "DataProtectionKeys" };

        using var pgConn = new NpgsqlConnection(_postgresConnectionString);
        await pgConn.OpenAsync();

        foreach (var table in tables)
        {
            try
            {
                var sql = $"SELECT setval(pg_get_serial_sequence('\"{table}\"', 'Id'), COALESCE((SELECT MAX(\"Id\") FROM \"{table}\"), 0) + 1, false)";
                using var cmd = new NpgsqlCommand(sql, pgConn);
                await cmd.ExecuteNonQueryAsync();
                log.AppendLine($"  Reset {table}_Id_seq");
            }
            catch (Exception ex)
            {
                log.AppendLine($"  {table}: {ex.Message}");
            }
        }

        return log.ToString();
    }

    public static async Task RunMigrationAsync()
    {
        var sqlitePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Data", "InvoiceManagement.db");
        if (!File.Exists(sqlitePath))
        {
            sqlitePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "InvoiceManagement.db");
        }

        var postgresConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=db.iqcazhhjufphlicmwtlr.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=6b0jhbwSMSI1wAi5;SSL Mode=Require;Trust Server Certificate=true";

        Console.WriteLine("=== SQLite to PostgreSQL Data Migration ===\n");

        if (!File.Exists(sqlitePath))
        {
            Console.WriteLine($"SQLite database not found at: {sqlitePath}");
            return;
        }

        var migrator = new DataMigrator(sqlitePath, postgresConnection);
        var result = await migrator.MigrateAllDataAsync();
        Console.WriteLine(result);
    }
}
