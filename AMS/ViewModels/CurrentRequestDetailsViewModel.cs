using AMS.Models;

namespace AMS.ViewModels
{
    public class CurrentRequestDetailsViewModel
    {
        public AmbulanceRequest Request { get; set; } = null!;

        public List<Hospital> Hospitals { get; set; }
            = new List<Hospital>();
    }
}

