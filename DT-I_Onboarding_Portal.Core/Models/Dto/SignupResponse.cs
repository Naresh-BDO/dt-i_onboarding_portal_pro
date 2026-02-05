namespace DT_I_Onboarding_Portal.Core.Models.Dto
{
    public class SignupResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}


