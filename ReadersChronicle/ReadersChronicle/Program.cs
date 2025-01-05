using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReadersChronicle.Data;
using ReadersChronicle.Hubs;
using ReadersChronicle.Services;
using ReadersChronicle.Settings;
using System.Text;

namespace ReadersChronicle
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            // Add Razor Pages and Controllers with Views
            builder.Services.AddRazorPages();
            builder.Services.AddControllersWithViews();

            // Add DbContext to the services
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddIdentity<User, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";  // Redirect to login page if not authenticated
        options.LogoutPath = "/Account/Logout"; // Redirect to logout path
        options.ExpireTimeSpan = TimeSpan.FromDays(1);  // Set token expiration
        options.SlidingExpiration = true; // Optional: resets the cookie expiration on each request
    });

            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
            builder.Services.AddHttpClient<BookService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<ArticleService>();
            builder.Services.AddScoped<CommentService>();
            builder.Services.AddScoped<FriendshipService>();
            builder.Services.AddScoped<MessagingService>();
            builder.Services.AddScoped<AdminService>();
            builder.Services.AddSignalR();

            builder.Services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = 104857600; // 100 MB
            });

            // or for Kestrel (if using Kestrel server)
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 104857600; // 100 MB
            });

            var app = builder.Build();


            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.MapHub<MessageHub>("/messageHub");

            app.UseAuthentication();  // Use Authentication middleware
            app.UseAuthorization();

            // Set up default controller routing
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapRazorPages();

            app.Run();
        }
    }
}
