using FoodManagementProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

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
            return View();
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

        //testing if db connection is established - yes
        public async Task<IActionResult> TestDb()
        {
            int count = await _context.Ingredients.CountAsync();
            return Content($"Ingredients in db {count}");
        }

    }
}
