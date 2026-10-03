using AMS.Models;

namespace AMS.ViewModels
{
    public class DriverDashboardViewModel
    {
        public Driver Driver { get; set; } = null!;

        public List<Ambulance> Ambulances { get; set; }
            = new List<Ambulance>();

        public List<AmbulanceRequest> Requests { get; set; }
            = new List<AmbulanceRequest>();

        public AmbulanceRequest? CurrentRequest { get; set; }

        public int RequestCount { get; set; }
    }
}