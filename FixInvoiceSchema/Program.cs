using Npgsql;

var connString = "Host=ep-cool-star-ahyxvsgm-pooler.c-3.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_WUyFsr3IiDR4;SSL Mode=Require";
var apiKey = "AIzaSyCSVhO-6baKAiExRQCsX6HCDIKsHB550Fg";

using var conn = new NpgsqlConnection(connString);
conn.Open();

Console.WriteLine("Setting Google AI API key in Neon SystemSettings...");

// Check if setting exists
using var checkCmd = new NpgsqlCommand(@"SELECT ""Id"" FROM ""SystemSettings"" WHERE ""SettingKey"" = 'GoogleAIApiKey'", conn);
var existingId = checkCmd.ExecuteScalar();

if (existingId != null)
{
    // Update existing setting
    using var updateCmd = new NpgsqlCommand(@"UPDATE ""SystemSettings"" SET ""SettingValue"" = @value, ""ModifiedDate"" = @date, ""ModifiedBy"" = 'System' WHERE ""SettingKey"" = 'GoogleAIApiKey'", conn);
    updateCmd.Parameters.AddWithValue("@value", apiKey);
    updateCmd.Parameters.AddWithValue("@date", DateTime.UtcNow);
    var rowsAffected = updateCmd.ExecuteNonQuery();
    Console.WriteLine($"Updated GoogleAIApiKey setting. Rows affected: {rowsAffected}");
}
else
{
    // Insert new setting
    using var insertCmd = new NpgsqlCommand(@"INSERT INTO ""SystemSettings"" (""SettingKey"", ""SettingValue"", ""Description"", ""Category"", ""ModifiedDate"", ""ModifiedBy"") VALUES ('GoogleAIApiKey', @value, 'Google AI (Gemini) API key for AI-powered invoice/payment import', 'API', @date, 'System')", conn);
    insertCmd.Parameters.AddWithValue("@value", apiKey);
    insertCmd.Parameters.AddWithValue("@date", DateTime.UtcNow);
    var rowsAffected = insertCmd.ExecuteNonQuery();
    Console.WriteLine($"Inserted GoogleAIApiKey setting. Rows affected: {rowsAffected}");
}

Console.WriteLine("\nAPI Key has been set in the Neon database!");
Console.WriteLine("Done!");
