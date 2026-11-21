using FoodManagementProject.Models;
using FoodManagementProject.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodManagementProject.Controllers
{
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AccountController : Controller
    {
        private readonly FoodManagementDbContext _context;
        private readonly IPasswordHasher<AppUser> _hasher;
        private readonly PasswordResetService _passwordResets;

        public AccountController(FoodManagementDbContext context, IPasswordHasher<AppUser> hasher, PasswordResetService passwordResets)
        {
            _context = context;
            _hasher = hasher;
            _passwordResets = passwordResets;
        }

        private static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        // for registration 
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
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
            return RedirectToAction("Index", "Home");
        }


        // for login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
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

            if (user == null || verification == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "This account has been deactivated.");
                return View(model);
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _hasher.HashPassword(user, model.Password);
                await _context.SaveChangesAsync();
            }

            await SignInUserAsync(user, model.RememberMe);

            if (user.UserRole == AppRoles.Regular && !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
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

        private Task SignInUserAsync(AppUser user, bool isPersistent)
        {
            return AuthSignIn.SignInAsync(HttpContext, user, isPersistent);
        }

        // for forgotten password
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _passwordResets.RequestAsync(model.Email, token => Url.Action("ResetPassword", "Account", new { token }, Request.Scheme)!);

            // the same answer whether or not the email belongs to an account
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string? token)
        {
            var valid = !string.IsNullOrEmpty(token) && await _passwordResets.IsValidAsync(token);
            return View(new ResetPasswordViewModel { Token = token ?? string.Empty, TokenIsValid = valid });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            // short password or passwords do not match
            if (!ModelState.IsValid)
            {
                model.TokenIsValid = await _passwordResets.IsValidAsync(model.Token);
                return View(model);
            }

            // link is expired, used, or was not sent
            if (!await _passwordResets.ResetAsync(model.Token, model.Password))
            {
                model.TokenIsValid = false;
                return View(model);
            }

            TempData["AccountMessage"] = "Your password has changed. Log in with the new password.";
            return RedirectToAction(nameof(Login));
        }
    }
}
