using Microsoft.AspNetCore.Mvc;
using ReadersChronicle.Models;
using ReadersChronicle.Services;

namespace ReadersChronicle.Controllers
{
    public class AdminController : Controller
    {
        public readonly AdminService _adminService;

        public AdminController(AdminService adminService)
        {
            _adminService = adminService;
        }
        public async Task<IActionResult> GetUsers()
        {
            var users =  await _adminService.GetAllUsers();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string username)
        {
            // If no username is provided, show all users
            var users = string.IsNullOrWhiteSpace(username)
                ? await _adminService.GetAllUsers()
                : await _adminService.SearchUsersAsync(username);

            return View("GetUsers", users);
        }

        public async Task<IActionResult> ChangeIsBlocked([FromBody] ChangeIsBlockedDto model)
        {
            if (string.IsNullOrEmpty(model?.Username))
            {
                return Json(new { success = false, message = "Username cannot be null or empty." });
            }

            var result = await _adminService.ChangeIsBlockedForUser(model.Username);

            if(result != true)
            {
                return Json(new { success = false, message = "Something went wrong" });
            }

            return Json(new { success = true, message = "User status updated successfully!" });
        }
    }
}
