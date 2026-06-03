using System.ComponentModel.DataAnnotations;

namespace QLTTYKPH.Models
{
    public class Class
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên lớp không được để trống")]
        [StringLength(100)]
        [Display(Name = "Tên lớp")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Survey> Surveys { get; set; } = new List<Survey>();
    }
}
