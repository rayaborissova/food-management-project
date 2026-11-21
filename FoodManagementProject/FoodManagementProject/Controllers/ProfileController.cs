using FoodManagementProject.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FoodManagementProject.Services;


namespace FoodManagementProject.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly FoodManagementDbContext _context;
        private readonly IPasswordHasher<AppUser> _hasher;

        public ProfileController(FoodManagementDbContext context, IPasswordHasher<AppUser> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task<ProfilePageViewModel> BuildPageAsync(ProfileDetailsViewModels? details = null)
        {
            var userId = CurrentUserId;
            var user = await _context.AppUsers.AsNoTracking().FirstAsync(u => u.UserId == userId);

            string? householdName = null;
            string? householdRole = null;

            //only registered user have a household
            if (user.UserRole == AppRoles.Regular)
            {
                var membership = await _context.HouseholdMembers.AsNoTracking().Include(m => m.Household).FirstOrDefaultAsync(m => m.UserId == userId);

                if( membership != null)
                {
                    householdName = membership.Household.PersonalUserId != null ? "Personal fridge" : membership.Household.HouseholdName;
                    householdRole = membership.HouseholdRole;
                }
            }

            return new ProfilePageViewModel
            {
                Details = details ?? new ProfileDetailsViewModels { DisplayName = user.DisplayName, Email = user.Email },
                Role = user.UserRole,
                Registered = user.DateCreated,
                HouseholdName = householdName,
                HouseholdRole = householdRole
            };
        }


        private async Task RefreshSignInAsync(AppUser user)
        {
            var current = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await AuthSignIn.SignInAsync(HttpContext, user, current.Properties?.IsPersistent ?? false);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await BuildPageAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDetails([Bind(Prefix = "Details")] ProfileDetailsViewModels form)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", await BuildPageAsync(form));
            }

            var user = await _context.AppUsers.FirstAsync(u => u.UserId == CurrentUserId);
            var displayName = form.DisplayName.Trim();
            var email = form.Email.Trim().ToLowerInvariant();

            if (displayName.Length == 0)
            {
                ModelState.AddModelError("Details.DisplayName", "Enter a display name.");
            }

            if (await _context.AppUsers.AnyAsync(u => u.Email == email && u.UserId != user.UserId))
            {
                ModelState.AddModelError("Details.Email", "Another account already uses this email.");
            }

            // to change the email, user needs to input the current password
            if (email != user.Email)
            {
                var passwordGood = !string.IsNullOrEmpty(form.CurrentPassword) && _hasher.VerifyHashedPassword(user, user.PasswordHash, form.CurrentPassword) != PasswordVerificationResult.Failed;

                if (!passwordGood)
                {
                    ModelState.AddModelError("Details.CurrentPassword", "Enter your current password to change your email.");
                }

            }

            if (!ModelState.IsValid)
            {
                return View("Index", await BuildPageAsync(form));
            }

            user.DisplayName = displayName;
            user.Email = email;
            await _context.SaveChangesAsync();

            await RefreshSignInAsync(user);

            TempData["ProfileMessage"] = "Your details were saved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult UpdateDetails()
        {
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordViewModel form)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", await BuildPageAsync());
            }

            var user = await _context.AppUsers.FirstAsync(u => u.UserId == CurrentUserId);

            if (_hasher.VerifyHashedPassword(user, user.PasswordHash, form.CurrentPassword) == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("Password.CurrentPassword", "The current password is incorrect.");
                return View("Index", await BuildPageAsync());
            }

            user.PasswordHash = _hasher.HashPassword(user, form.NewPassword);
            user.SecurityStamp = Guid.NewGuid(); // signs out every session of this account
            await _context.SaveChangesAsync();

            await RefreshSignInAsync(user);

            TempData["ProfileMessage"] = "Your password was changed. Other devices have been signed out";
            return RedirectToAction(nameof(Index));
        }
    }
}
