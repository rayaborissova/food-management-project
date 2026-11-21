using Microsoft.AspNetCore.Mvc;

namespace FoodManagementProject.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
