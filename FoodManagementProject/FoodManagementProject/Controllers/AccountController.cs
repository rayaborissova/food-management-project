using System.Security.Claims;
using FoodManagementProject.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32.SafeHandles;

namespace FoodManagementProject.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly FoodManagementDbContext _context;
        private readonly IPasswordHasher<AppUser> _hasher;

        public AccountController(FoodManagementDbContext context, IPasswordHasher<AppUser> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        private static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        // for registration 
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Inventory");
            }
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim().ToLowerInvariant();

            if (await _context.AppUsers.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
                return View(model);
            }

            var user = new AppUser
            {
                Email = email,
                DisplayName = model.DisplayName.Trim(),
                UserRole = "regular",
                DateCreated = Now
            };

            user.PasswordHash = _hasher.HashPassword(user, model.Password);

            // user, personal household and membership are created together 
            await using var transaction = await _context.Database.BeginTransactionAsync();

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync(); // generating user.UserId 

            var household = new Household
            {
                HouseholdName = $"{user.DisplayName}'s fridge",
                DateCreated = Now,
                PersonalUserId = user.UserId
            };
            _context.Households.Add(household);
            await _context.SaveChangesAsync();

            _context.HouseholdMembers.Add(new HouseholdMember
            {
                UserId = user.UserId,
                HouseholdId = household.HouseholdId,
                HouseholdRole = "owner",
                DateJoined = Now
            });
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await SignInUserAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Inventory");
        }


        // for login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Inventory");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid) 
            { 
                return View(model);
            }

            var email = model.Email.Trim().ToLowerInvariant();
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == email);

            var verification = user == null ? PasswordVerificationResult.Failed : _hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _hasher.HashPassword(user, model.Password);
                await _context.SaveChangesAsync();
            }

            await SignInUserAsync(user, model.RememberMe);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction("Index", "Inventory");
        }

        // for logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SignInUserAsync(AppUser user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.DisplayName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.UserRole)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = isPersistent });
        }
    }
}
