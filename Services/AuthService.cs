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
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Status == "Active");

            if (user != null)
            {
                // User must have a password set
                if (string.IsNullOrEmpty(user.PasswordHash))
                {
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
                    }
                    else
                    {
                        // Legacy plain text password - verify and upgrade to BCrypt
                        if (user.PasswordHash == password)
                        {
                            // Upgrade to BCrypt hash
                            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                            isValidPassword = true;
                        }
                    }
                }
                catch
                {
                    return null;
                }

                if (isValidPassword)
                {
                    user.LastLoginDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return user;
                }
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

