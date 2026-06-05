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

            bool isAdmin = SessionHelper.IsAdmin(HttpContext.Session);
            int? deptId = HttpContext.Session.GetInt32("DepartmentId");

            var surveysQuery = _db.Surveys.AsQueryable();
            var feedbacksQuery = _db.Feedbacks.Include(f => f.Survey).AsQueryable();

            if (!isAdmin && deptId.HasValue)
            {
                surveysQuery = surveysQuery.Where(s => s.DepartmentId == deptId.Value);
                feedbacksQuery = feedbacksQuery.Where(f => f.Survey != null && f.Survey.DepartmentId == deptId.Value);
            }

            ViewBag.TotalSurveys = await surveysQuery.CountAsync();
            ViewBag.PublishedSurveys = await surveysQuery.CountAsync(s => s.IsPublished);
            ViewBag.TotalFeedbacks = await feedbacksQuery.CountAsync();
            ViewBag.NewFeedbacks = await feedbacksQuery.CountAsync(f => f.Status == FeedbackStatus.New);
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
