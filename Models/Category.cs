using System.ComponentModel.DataAnnotations;

namespace QLTTYKPH.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(200)]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = string.Empty;

        public ICollection<Survey> Surveys { get; set; } = new List<Survey>();
    }
}
