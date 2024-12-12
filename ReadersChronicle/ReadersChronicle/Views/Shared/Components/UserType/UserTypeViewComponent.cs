using Microsoft.AspNetCore.Mvc;
using ReadersChronicle.Data;
using System.Security.Claims;

namespace ReadersChronicle.Views.Shared.Components.UserType
{
    public class UserTypeViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public UserTypeViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return View<string>("Default", null); // Return null for unauthenticated users
            }

            var user = await _context.Users.FindAsync(userId);
            var userType = user?.UserType;

            return View("Default", userType); // Pass userType to the view
        }
    }
}
