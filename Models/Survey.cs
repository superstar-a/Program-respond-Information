using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLTTYKPH.Models
{
    public class Survey
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(300)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Đã xuất bản")]
        public bool IsPublished { get; set; } = false;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Ngày xuất bản")]
        public DateTime? PublishedAt { get; set; }

        [Display(Name = "Ngày đóng")]
        public DateTime? ClosedAt { get; set; }

        [Display(Name = "Thời gian mở")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Thời gian kết thúc")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Tệp đính kèm")]
        public string? AttachmentPath { get; set; }

        [Display(Name = "Lớp mục tiêu")]
        public int? ClassId { get; set; }

        [ForeignKey("ClassId")]
        public Class? Class { get; set; }

        [Display(Name = "Danh mục")]
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [Display(Name = "Phòng ban")]
        public int? DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }

        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    }
}
