using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FoodManagementProject.Models;

namespace FoodManagementProject.Controllers
{
    [Authorize(Roles = AppRoles.Moderator)]
    public class ModerationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
