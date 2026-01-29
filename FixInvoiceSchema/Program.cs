using Npgsql;

var connString = "Host=ep-cool-star-ahyxvsgm-pooler.c-3.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_WUyFsr3IiDR4;SSL Mode=Require";
var apiKey = "AIzaSyCSVhO-6baKAiExRQCsX6HCDIKsHB550Fg";

using var conn = new NpgsqlConnection(connString);
conn.Open();

Console.WriteLine("Checking Google AI API key in Neon SystemSettings...");

// First, check what's in the database
using var readCmd = new NpgsqlCommand(@"SELECT ""Id"", ""SettingKey"", ""SettingValue"", ""Category"" FROM ""SystemSettings"" WHERE ""SettingKey"" = 'GoogleAIApiKey'", conn);
using var reader = readCmd.ExecuteReader();

if (reader.Read())
{
    var id = reader.GetInt32(0);
    var key = reader.GetString(1);
    var value = reader.IsDBNull(2) ? "(null)" : reader.GetString(2);
    var category = reader.IsDBNull(3) ? "(null)" : reader.GetString(3);
    Console.WriteLine($"Found existing setting:");
    Console.WriteLine($"  Id: {id}");
    Console.WriteLine($"  Key: {key}");
    Console.WriteLine($"  Value: {(value.Length > 20 ? value.Substring(0, 20) + "..." : value)}");
    Console.WriteLine($"  Category: {category}");
    reader.Close();

    // Update the value
    using var updateCmd = new NpgsqlCommand(@"UPDATE ""SystemSettings"" SET ""SettingValue"" = @value, ""ModifiedDate"" = @date, ""ModifiedBy"" = 'System' WHERE ""SettingKey"" = 'GoogleAIApiKey'", conn);
    updateCmd.Parameters.AddWithValue("@value", apiKey);
    updateCmd.Parameters.AddWithValue("@date", DateTime.UtcNow);
    var rowsAffected = updateCmd.ExecuteNonQuery();
    Console.WriteLine($"\nUpdated GoogleAIApiKey setting. Rows affected: {rowsAffected}");
}
else
{
    reader.Close();
    Console.WriteLine("Setting not found, inserting new...");

    // Insert new setting
    using var insertCmd = new NpgsqlCommand(@"INSERT INTO ""SystemSettings"" (""SettingKey"", ""SettingValue"", ""Description"", ""Category"", ""ModifiedDate"", ""ModifiedBy"") VALUES ('GoogleAIApiKey', @value, 'Google AI (Gemini) API key for AI-powered invoice/payment import', 'API', @date, 'System')", conn);
    insertCmd.Parameters.AddWithValue("@value", apiKey);
    insertCmd.Parameters.AddWithValue("@date", DateTime.UtcNow);
    var rowsAffected = insertCmd.ExecuteNonQuery();
    Console.WriteLine($"Inserted GoogleAIApiKey setting. Rows affected: {rowsAffected}");
}

// Verify the value
Console.WriteLine("\nVerifying...");
using var verifyCmd = new NpgsqlCommand(@"SELECT ""SettingValue"" FROM ""SystemSettings"" WHERE ""SettingKey"" = 'GoogleAIApiKey'", conn);
var finalValue = verifyCmd.ExecuteScalar() as string;
Console.WriteLine($"Final value in database: {(finalValue != null && finalValue.Length > 20 ? finalValue.Substring(0, 20) + "..." : finalValue ?? "(null)")}");
Console.WriteLine($"Expected value starts with: {apiKey.Substring(0, 20)}...");
Console.WriteLine($"Match: {finalValue == apiKey}");

Console.WriteLine("\nDone!");
