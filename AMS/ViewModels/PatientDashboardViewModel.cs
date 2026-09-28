using AMS.Models;

namespace AMS.ViewModels
{
    public class PatientDashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;

        public List<AmbulanceRequest> RecentRequests { get; set; } = new();
    }
}