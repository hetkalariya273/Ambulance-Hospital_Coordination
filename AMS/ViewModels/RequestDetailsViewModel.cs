using AMS.Models;

namespace AMS.ViewModels
{
    public class RequestDetailsViewModel
    {
        public AmbulanceRequest Request { get; set; } = null!;

        public List<Ambulance> AvailableAmbulances { get; set; }
            = new List<Ambulance>();
    }
}