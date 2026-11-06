using FoodManagementProject.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace FoodManagementProject.Controllers
{
    public abstract class HouseholdControllerBase : Controller
    {
        
        protected int UserId { get; private set; }
        protected int HouseholdId { get; private set; }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                context.Result = new ChallengeResult();
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<FoodManagementDbContext>(); //connecting to the database

            var householdId = await db.HouseholdMembers.Where(m => m.UserId == userId).Select(m => (int?)m.HouseholdId).FirstOrDefaultAsync(); // selecting the household id that the user belongs to

            if (householdId == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                context.Result = RedirectToAction("Login", "Account"); // user redirected to login if no household is detected
                return;
            }

            UserId = userId;
            HouseholdId = householdId.Value;

            await next();
        }

    }
}
