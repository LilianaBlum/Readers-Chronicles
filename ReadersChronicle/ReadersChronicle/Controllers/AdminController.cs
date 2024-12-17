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
