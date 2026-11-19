using System.Security.Claims;
using FoodManagementProject.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FoodManagementProject.Services
{
    public class AuthSignIn
    {
        public const string StampClaim = "security_stamp";

        public static Task SignInAsync(HttpContext httpContext, AppUser user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.DisplayName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.UserRole),
                new(StampClaim, user.SecurityStamp.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            return httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = isPersistent });
        }
    }
}
