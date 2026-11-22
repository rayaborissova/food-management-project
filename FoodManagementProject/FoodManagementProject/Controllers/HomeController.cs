using FoodManagementProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace FoodManagementProject.Controllers
{
    public class HomeController : Controller
    {

        // adding the db context
        private readonly FoodManagementDbContext _context;

        public HomeController(FoodManagementDbContext context)
        {
            _context = context;
        }
        
        public IActionResult Index() 
        {
            if (User.Identity?.IsAuthenticated != true) // to prevent a dead-end landing page
            {
                return RedirectToAction("Login", "Account");
            }
            
            // land page for admin
            if(User.IsInRole(AppRoles.Admin))
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            // land page for moderator
            if (User.IsInRole(AppRoles.Moderator))
            {
                return RedirectToAction("Dashboard", "Moderation");
            }

            return RedirectToAction("Index", "Inventory");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
