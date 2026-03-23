using InvoiceManagement.Data;
using InvoiceManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceManagement.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            Console.WriteLine($"[AUTH] Login attempt for: '{username}'");

            // Try to find user by username OR email (case-insensitive)
            var lowerUsername = username.ToLower();
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    (u.Username.ToLower() == lowerUsername || u.Email.ToLower() == lowerUsername) &&
                    u.Status == "Active");

            if (user != null)
            {
                Console.WriteLine($"[AUTH] Found user: Id={user.Id}, Username='{user.Username}', Email='{user.Email}', Status='{user.Status}', HasPasswordHash={!string.IsNullOrEmpty(user.PasswordHash)}, HashLength={user.PasswordHash?.Length ?? 0}");

                // User must have a password set
                if (string.IsNullOrEmpty(user.PasswordHash))
                {
                    Console.WriteLine($"[AUTH] FAILED - No password hash set for user '{user.Username}'");
                    return null; // No password set, cannot authenticate
                }

                // Verify password using BCrypt
                bool isValidPassword = false;
                try
                {
                    // Check if it's a BCrypt hash (starts with $2)
                    if (user.PasswordHash.StartsWith("$2"))
                    {
                        isValidPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                        Console.WriteLine($"[AUTH] BCrypt verify result: {isValidPassword} for user '{user.Username}'");
                    }
                    else
                    {
                        // Legacy plain text password - verify and upgrade to BCrypt
                        if (user.PasswordHash == password)
                        {
                            // Upgrade to BCrypt hash
                            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                            isValidPassword = true;
                            Console.WriteLine($"[AUTH] Legacy password matched for '{user.Username}', upgraded to BCrypt");
                        }
                        else
                        {
                            Console.WriteLine($"[AUTH] Legacy password did NOT match for '{user.Username}'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AUTH] Password verification ERROR for '{user.Username}': {ex.Message}");
                    return null;
                }

                if (isValidPassword)
                {
                    Console.WriteLine($"[AUTH] SUCCESS - User '{user.Username}' authenticated");
                    user.LastLoginDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return user;
                }
                else
                {
                    Console.WriteLine($"[AUTH] FAILED - Invalid password for user '{user.Username}'");
                }
            }
            else
            {
                Console.WriteLine($"[AUTH] No active user found matching: '{username}'");
            }

            return null;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }
    }
}

