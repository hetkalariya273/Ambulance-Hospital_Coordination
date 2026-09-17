using System.ComponentModel.DataAnnotations;

namespace AMS.Enums
{
    public enum RequestStatus
    {
        [Display(Name = "Request Submitted")]
        Requested,

        [Display(Name = "Finding Ambulance")]
        Searching,

        [Display(Name = "Ambulance Assigned")]
        Assigned,

        [Display(Name = "Driver Accepted")]
        Accepted,

        [Display(Name = "Driver On The Way")]
        OnTheWay,

        [Display(Name = "Driver Reached Patient")]
        ReachedPatient,

        [Display(Name = "Patient Picked Up")]
        PatientPickedUp,

        [Display(Name = "Going To Hospital")]
        GoingToHospital,

        [Display(Name = "Reached Hospital")]
        ReachedHospital,

        [Display(Name = "Completed")]
        Completed,

        [Display(Name = "Cancelled")]
        Cancelled
    }
}