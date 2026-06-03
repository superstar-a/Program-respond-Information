using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLTTYKPH.Models
{
    public enum ComplaintStatus
    {
        [Display(Name = "Mới")]
        New = 0,
        [Display(Name = "Đang xử lý")]
        Processing = 1,
        [Display(Name = "Đã giải quyết")]
        Resolved = 2
    }

    public class Complaint
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề khiếu nại không được để trống")]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung khiếu nại không được để trống")]
        [Display(Name = "Nội dung")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Thời gian gửi")]
        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        [Display(Name = "Trạng thái")]
        public ComplaintStatus Status { get; set; } = ComplaintStatus.New;

        [Display(Name = "Gửi ẩn danh")]
        public bool IsAnonymous { get; set; } = false;

        [Required]
        [Display(Name = "Người khiếu nại")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng ban tiếp nhận")]
        [Display(Name = "Phòng ban tiếp nhận")]
        public int DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }

        [Display(Name = "Nội dung phản hồi/giải quyết")]
        public string? ResolutionNote { get; set; }

        [Display(Name = "Thời gian giải quyết")]
        public DateTime? ResolvedAt { get; set; }

        [Display(Name = "Người giải quyết")]
        public int? ResolvedByUserId { get; set; }

        [ForeignKey("ResolvedByUserId")]
        public User? ResolvedByUser { get; set; }
    }
}
