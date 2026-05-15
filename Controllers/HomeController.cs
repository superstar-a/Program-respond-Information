using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLTTYKPH.Data;
using QLTTYKPH.Helpers;
using QLTTYKPH.Models;

namespace QLTTYKPH.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            ViewBag.TotalSurveys = await _db.Surveys.CountAsync();
            ViewBag.PublishedSurveys = await _db.Surveys.CountAsync(s => s.IsPublished);
            ViewBag.TotalFeedbacks = await _db.Feedbacks.CountAsync();
            ViewBag.NewFeedbacks = await _db.Feedbacks.CountAsync(f => f.Status == FeedbackStatus.New);
            ViewBag.TotalUsers = await _db.Users.CountAsync();
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
