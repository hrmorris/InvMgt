using Npgsql;

var connString = "Host=ep-cool-star-ahyxvsgm-pooler.c-3.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_WUyFsr3IiDR4;SSL Mode=Require";

using var conn = new NpgsqlConnection(connString);
conn.Open();

// Reset password for gegi@ecpnghs.org to "Password123!"
var newPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");

Console.WriteLine("Resetting password for gegi@ecpnghs.org on Neon...");

using var updateCmd = new NpgsqlCommand(@"UPDATE ""Users"" SET ""PasswordHash"" = @hash WHERE ""Username"" = @username", conn);
updateCmd.Parameters.AddWithValue("@hash", newPasswordHash);
updateCmd.Parameters.AddWithValue("@username", "gegi@ecpnghs.org");
var rowsAffected = updateCmd.ExecuteNonQuery();

Console.WriteLine($"Password reset for gegi@ecpnghs.org. Rows affected: {rowsAffected}");
Console.WriteLine("New password: Password123!");
Console.WriteLine("\nDone!");
