using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLTTYKPH.Data;
using QLTTYKPH.Helpers;
using QLTTYKPH.Models;

namespace QLTTYKPH.Controllers
{
    public class ProcessingController : Controller
    {
        private readonly AppDbContext _db;

        public ProcessingController(AppDbContext db)
        {
            _db = db;
        }

        private IActionResult RequireStaffOrAdmin()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (SessionHelper.IsStudent(HttpContext.Session))
                return RedirectToAction("Index", "Home");
            return null!;
        }

        // Thêm bản ghi xử lý và cập nhật trạng thái
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRecord(int feedbackId, string action, string newStatus)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            if (string.IsNullOrWhiteSpace(action))
            {
                TempData["Error"] = "Nội dung xử lý không được để trống!";
                return RedirectToAction("Details", "Feedback", new { id = feedbackId });
            }

            int handlerId = SessionHelper.GetUserId(HttpContext.Session)!.Value;

            var record = new ProcessingRecord
            {
                FeedbackId = feedbackId,
                Action = action,
                CreatedAt = DateTime.Now,
                HandlerUserId = handlerId
            };
            _db.ProcessingRecords.Add(record);

            if (Enum.TryParse<FeedbackStatus>(newStatus, out var statusEnum))
            {
                var feedback = await _db.Feedbacks.FindAsync(feedbackId);
                if (feedback != null)
                    feedback.Status = statusEnum;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Cập nhật xử lý thành công!";
            return RedirectToAction("Details", "Feedback", new { id = feedbackId });
        }

        // Xem lịch sử xử lý của một phản hồi
        public async Task<IActionResult> History(int feedbackId)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var records = await _db.ProcessingRecords
                .Include(pr => pr.HandlerUser)
                .Include(pr => pr.Feedback)
                    .ThenInclude(f => f!.Survey)
                .Where(pr => pr.FeedbackId == feedbackId)
                .OrderBy(pr => pr.CreatedAt)
                .ToListAsync();

            ViewBag.FeedbackId = feedbackId;
            return View(records);
        }

        // Xóa bản ghi xử lý
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRecord(int id)
        {
            var redirect = RequireStaffOrAdmin();
            if (redirect != null) return redirect;

            var record = await _db.ProcessingRecords.FindAsync(id);
            if (record == null) return NotFound();
            int feedbackId = record.FeedbackId;
            _db.ProcessingRecords.Remove(record);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Xóa bản ghi xử lý thành công!";
            return RedirectToAction("Details", "Feedback", new { id = feedbackId });
        }
    }
}
