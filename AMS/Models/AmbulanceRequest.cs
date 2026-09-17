using System.ComponentModel.DataAnnotations;
using AMS.Enums;

namespace AMS.Models
{
    public class AmbulanceRequest
    {
        [Key]
        public int RequestId { get; set; }

        // Patient who created the request
        // Identity by default uses string For ID
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }


        public int? DriverId { get; set; }
        public Driver? Driver { get; set; }


        // Ambulance assigned to this request
        public int? AmbulanceId { get; set; }
        public Ambulance? Ambulance { get; set; }


        // Hospital selected for the patient
        public int? HospitalId { get; set; }
        public Hospital? Hospital { get; set; }


        // Patient pickup location
        public double PickupLatitude { get; set; }

        public double PickupLongitude { get; set; }

        // Emergency information
        [Required]
        public EmergencyType EmergencyType { get; set; }

        [Required]
        public PatientCondition PatientCondition { get; set; }

        [Range(1, 20)]
        public int NumberOfPatients { get; set; } = 1;

        // Request status
        public RequestStatus Status { get; set; } = RequestStatus.Requested;

        public DateTime RequestTime { get; set; } = DateTime.Now;
    }
}
