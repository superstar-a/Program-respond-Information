using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLTTYKPH.Models
{
    public class ProcessingRecord
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nội dung hành động không được để trống")]
        [Display(Name = "Nội dung xử lý")]
        public string Action { get; set; } = string.Empty;

        [Display(Name = "Ghi chú")]
        [StringLength(500)]
        public string? Note { get; set; }

        [Display(Name = "Thời gian")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public int FeedbackId { get; set; }

        [ForeignKey("FeedbackId")]
        public Feedback? Feedback { get; set; }

        [Required]
        public int HandlerUserId { get; set; }

        [ForeignKey("HandlerUserId")]
        public User? HandlerUser { get; set; }
    }
}
