using Npgsql;

var connStr = "Host=db.iqcazhhjufphlicmwtlr.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=6b0jhbwSMSI1wAi5;SSL Mode=Require;Trust Server Certificate=true;Timeout=30;";
var builder = new NpgsqlConnectionStringBuilder(connStr);
using var conn = new NpgsqlConnection(builder.ConnectionString);
await conn.OpenAsync();
Console.WriteLine("Connected to database.");

// 1. Check document counts and content status
using (var cmd = new NpgsqlCommand(@"
    SELECT 
        COUNT(*) as total_docs,
        SUM(CASE WHEN ""FileContent"" IS NULL OR length(""FileContent"") = 0 THEN 1 ELSE 0 END) as empty_content,
        SUM(CASE WHEN ""FileContent"" IS NOT NULL AND length(""FileContent"") > 0 THEN 1 ELSE 0 END) as has_content
    FROM ""ImportedDocuments"";
", conn))
{
    using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        Console.WriteLine($"Total documents: {reader["total_docs"]}");
        Console.WriteLine($"Empty/null content: {reader["empty_content"]}");
        Console.WriteLine($"Has content: {reader["has_content"]}");
    }
}

// 2. Show sample of documents
using (var cmd2 = new NpgsqlCommand(@"
    SELECT ""Id"", ""OriginalFileName"", ""ContentType"", ""FileSize"", 
           length(""FileContent"") as actual_content_length,
           ""ProcessingStatus"", ""DocumentType"", ""InvoiceId"", ""UploadDate""
    FROM ""ImportedDocuments""
    ORDER BY ""Id"" DESC
    LIMIT 10;
", conn))
{
    using var reader2 = await cmd2.ExecuteReaderAsync();
    Console.WriteLine("\n--- Last 10 documents ---");
    while (await reader2.ReadAsync())
    {
        var id = reader2["Id"];
        var name = reader2["OriginalFileName"];
        var size = reader2["FileSize"];
        var contentLen = reader2["actual_content_length"];
        var status = reader2["ProcessingStatus"];
        var invId = reader2["InvoiceId"];
        Console.WriteLine($"  ID={id}, File={name}, Size={size}, ContentLen={contentLen}, Status={status}, InvoiceId={invId}");
    }
}

// 3. Check if there's a neon_backup that could have documents
using (var cmd3 = new NpgsqlCommand(@"
    SELECT COUNT(*) FROM ""ImportedDocuments"" WHERE ""FileContent"" IS NOT NULL AND length(""FileContent"") > 0;
", conn))
{
    var count = await cmd3.ExecuteScalarAsync();
    Console.WriteLine($"\nDocuments with actual file content: {count}");
}

Console.WriteLine("\nDone.");
