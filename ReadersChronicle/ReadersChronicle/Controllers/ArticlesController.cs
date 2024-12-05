﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReadersChronicle.Data;
using ReadersChronicle.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

[Authorize]
public class ArticlesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public ArticlesController(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string viewType = "All")
    {
        // Get the current user
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account"); // Redirect to login if user is not authenticated
        }

        // Get all articles from the database
        var allArticles = await _context.Articles.Include(a => a.User)
                                                 .OrderByDescending(a => a.TimeCreated)
                                                 .ToListAsync();

        // Filter articles based on the viewType
        var filteredArticles = viewType == "My"
            ? allArticles.Where(a => a.UserId == user.Id).ToList()
            : allArticles;

        // Build the ViewModel
        var viewModel = new ArticlesIndexViewModel
        {
            Articles = filteredArticles,
            UserBooks = await _context.UserBooks.Where(ub => ub.UserID == user.Id).ToListAsync()
        };

        ViewData["ViewType"] = viewType; // To highlight the active navigation item

        return View(viewModel);
    }



    [HttpPost]
    public async Task<IActionResult> Create(CreateArticleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("Index");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userBook = await _context.UserBooks.FirstOrDefaultAsync(ub => ub.UserBookID == model.UserBookID && ub.UserID == userId);

        if (userBook == null)
        {
            return NotFound("Book not found in user's library.");
        }

        var article = new Article
        {
            UserId = userId,
            Title = model.Title,
            Description = model.Description,
            Picture = userBook.Picture,
            PictureMimeType = userBook.PictureMimeType,
            TimeCreated = DateTime.UtcNow
        };

        _context.Articles.Add(article);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        // Get the current user
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account"); // Redirect to login if user is not authenticated
        }

        // Find the article by ID
        var article = await _context.Articles.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

        if (article == null)
        {
            return NotFound("Article not found or you don't have permission to delete it.");
        }

        // Remove the article
        _context.Articles.Remove(article);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { viewType = "My" });
    }

}
