using AMS.Models;

namespace AMS.ViewModels
{
    public class DriverDashboardViewModel
    {
        public Driver Driver { get; set; } = null!;

        public Ambulance? Ambulance { get; set; }

        public AmbulanceRequest? ActiveRequest { get; set; }

        public int ActiveRequestCount { get; set; }
    }
}