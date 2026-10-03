using System.ComponentModel.DataAnnotations;

namespace AMS.ViewModels
{
    public class AmbulanceAssignmentViewModel
    {
        public int AmbulanceId { get; set; }

        public string VehicleNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a driver.")]
        public int? DriverId { get; set; }
    }
}