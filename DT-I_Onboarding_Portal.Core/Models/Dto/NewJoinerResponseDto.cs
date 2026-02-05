namespace DT_I_Onboarding_Portal.Core.Models.Dto
{
    public class NewJoinerResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? ManagerName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? WelcomeEmailSentAtUtc { get; set; }
        public string? LastSendError { get; set; }
    }
}
