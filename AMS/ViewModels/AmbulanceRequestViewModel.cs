using AMS.Enums;
using System.ComponentModel.DataAnnotations;

namespace AMS.ViewModels
{
    public class AmbulanceRequestViewModel
    {
        [Required]
        [Display(Name = "Emergency Type")]
        public EmergencyType EmergencyType { get; set; }

        [Required]
        [Display(Name = "Patient Condition")]
        public PatientCondition PatientCondition { get; set; }

        [Range(1, 20)]
        [Display(Name = "Number of Patients")]
        public int NumberOfPatients { get; set; } = 1;

        // Current location obtained from browser
        [Required]
        public double PickupLatitude { get; set; }

        [Required]
        public double PickupLongitude { get; set; }
    }
}
