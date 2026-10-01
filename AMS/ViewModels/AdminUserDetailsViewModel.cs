namespace AMS.ViewModels
{
    public class AdminUserDetailsViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public int TotalRequests { get; set; }

        public int ActiveRequests { get; set; }

        public int CompletedRequests { get; set; }

        public int CancelledRequests { get; set; }
    }
}