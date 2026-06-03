using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLTTYKPH.Models
{
    public enum UserRole
    {
        Admin = 0,
        Staff = 1,
        Student = 2
    }

    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(100)]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(256)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(200)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Vai trò")]
        public UserRole Role { get; set; } = UserRole.Student;

        [Display(Name = "Lớp")]
        public int? ClassId { get; set; }

        [ForeignKey("ClassId")]
        public Class? Class { get; set; }

        [StringLength(200)]
        [Display(Name = "Phòng ban")]
        public string? Department { get; set; }

        [Display(Name = "Yêu cầu đổi mật khẩu")]
        public bool MustChangePassword { get; set; } = false;

        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
        public ICollection<ProcessingRecord> ProcessingRecords { get; set; } = new List<ProcessingRecord>();
    }
}
