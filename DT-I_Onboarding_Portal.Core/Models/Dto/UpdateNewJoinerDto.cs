using System.ComponentModel.DataAnnotations;

namespace DT_I_Onboarding_Portal.Core.Models.Dto
{
    public class UpdateNewJoinerDto
    {
        [Required, StringLength(120)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(120)]
        public string? Department { get; set; }

        [StringLength(120)]
        public string? ManagerName { get; set; }

        [Required]
        public DateTime StartDate { get; set; }
    }
}
