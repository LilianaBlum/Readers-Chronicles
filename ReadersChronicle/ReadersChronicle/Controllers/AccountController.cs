using Microsoft.AspNetCore.Mvc;
using ReadersChronicle.Models;

namespace ReadersChronicle.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account/Login
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Handle login logic here
                // Check if the user exists and redirect accordingly
                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Handle registration logic here
                // Add user to database, etc.
                return RedirectToAction("index", "Home");
            }
            return View(model);
        }
    }
}