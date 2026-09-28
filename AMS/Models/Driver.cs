using System.ComponentModel.DataAnnotations;

namespace AMS.Models
{
    public class Driver
    {
        public int DriverId { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string LicenseNumber { get; set; } = string.Empty;

        public bool IsAvailable { get; set; }

        // Identity account linked to this driver
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        // Ambulance assigned to this driver
        public ICollection<Ambulance> Ambulances { get; set; }
            = new List<Ambulance>();

        // Requests handled by this driver
        public ICollection<AmbulanceRequest> AmbulanceRequests { get; set; }
            = new List<AmbulanceRequest>();
    }
}