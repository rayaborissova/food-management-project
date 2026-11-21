using System.Text;
using FoodManagementProject.Models;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace FoodManagementProject.Services
{
    public class PasswordResetService
    {
        private static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);
        private static readonly TimeSpan MinGap = TimeSpan.FromMinutes(1);


        private readonly FoodManagementDbContext _context;
        private readonly IPasswordHasher<AppUser> _hasher;
        private readonly IEmailSender _email;
        private readonly ILogger<PasswordResetService> _logger;

        public PasswordResetService(FoodManagementDbContext context, IPasswordHasher<AppUser> hasher, IEmailSender email, ILogger<PasswordResetService> logger)
        {
            _context = context;
            _hasher = hasher;
            _email = email;
            _logger = logger;
        }

        private static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        // storing only the token hash in the databasel the email link contains the original token
        private static string Hash(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        }

        // creating and email a reset link for an active account
        public async Task RequestAsync(string email, Func<string, string> buildLink)
        {
            var normalized = email.Trim().ToLowerInvariant();
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == normalized && u.IsActive);

            if (user == null)
            {
                return;
            }

            var now = Now;
            var recent = now - MinGap;

            // skip this request if the account already had a reset request in the last minute
            if (await _context.PasswordResets.AnyAsync(r => r.UserId == user.UserId && r.DateCreated > recent))
            {
                return;
            }

            // remove previous reset records for this account before saving the new token
            await _context.PasswordResets.Where(r => r.UserId == user.UserId).ExecuteDeleteAsync();

            var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

            _context.PasswordResets.Add(new PasswordReset
            {
                UserId = user.UserId,
                TokenHash = Hash(token),
                DateCreated = now,
                ExpiresAt = now + Lifetime
            });
            await _context.SaveChangesAsync();

            var body = $"Hello {user.DisplayName},\n\n"
                + "Someone asked to reset the password of your account. Use this link within 1 hour:\n\n"
                + $"{buildLink(token)}\n\n"
                + "If this wasn't you, ignore this message and your password stays the same.";

            try
            {
                await _email.SendAsync(user.Email, "Reset your password", body);
            }
            catch (Exception ex)
            {
                // the user sees the same confirmation either way
                _logger.LogError(ex, "Sending the password reset email failed");
            }
        }

        private async Task<PasswordReset?> FindValidAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var hash = Hash(token);
            var now = Now;

            return await _context.PasswordResets.Include(r => r.User).FirstOrDefaultAsync(r => r.TokenHash == hash && r.UsedAt == null && r.ExpiresAt > now && r.User.IsActive);
        }

        public async Task<bool> IsValidAsync(string token)
        { 
            return await FindValidAsync(token) != null; // look up the unused, expired reset token belonging to an active account
        }

        public async Task<bool> ResetAsync(string token, string newPassword)
        {
            var reset = await FindValidAsync(token);
            if (reset == null)
            {
                return false;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(); // save the token claim and password change in the same transaction

            // claiming the token first so a link can only be used once
            var now = Now;
            var claimed = await _context.PasswordResets.Where(r => r.ResetId == reset.ResetId && r.UsedAt == null).ExecuteUpdateAsync(s => s.SetProperty(r => r.UsedAt, (DateTime?)now));

            if (claimed == 0)
            {
                await transaction.RollbackAsync();
                return false;
            }

            reset.User.PasswordHash = _hasher.HashPassword(reset.User, newPassword);
            reset.User.SecurityStamp = Guid.NewGuid(); // signs the account out everywhere
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return true;
        }
    }
}
