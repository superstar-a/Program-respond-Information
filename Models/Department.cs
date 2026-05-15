using System.ComponentModel.DataAnnotations;

namespace QLTTYKPH.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên phòng ban không được để trống")]
        [StringLength(200)]
        [Display(Name = "Tên phòng ban")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        public ICollection<Survey> Surveys { get; set; } = new List<Survey>();
    }
}
