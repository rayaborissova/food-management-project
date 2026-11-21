using System.Security.Claims;
using FoodManagementProject.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace FoodManagementProject.Services
{
    public static class AuthCookieEvents
    {
        //for every authenticated request and rejects the session for deleted or deactivated accounts, or accounts that have had role changes 
        public static async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            if(!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                await RejectAsync(context);
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<FoodManagementDbContext>();

            var user = await db.AppUsers.AsNoTracking().Where(u => u.UserId == userId).Select(u => new { u.IsActive, u.UserRole, u.SecurityStamp }).FirstOrDefaultAsync();

            if (user == null || !user.IsActive || user.UserRole != context.Principal!.FindFirstValue(ClaimTypes.Role) || user.SecurityStamp.ToString() != context.Principal.FindFirstValue(AuthSignIn.StampClaim))
            {
                await RejectAsync(context);
            }
        }

        private static async Task RejectAsync(CookieValidatePrincipalContext context)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
