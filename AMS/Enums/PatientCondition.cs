using System.ComponentModel.DataAnnotations;

namespace AMS.Enums
{
    public enum PatientCondition
    {
        [Display(Name = "Stable")]
        Stable,

        [Display(Name = "Serious")]
        Serious,

        [Display(Name = "Critical")]
        Critical,

        [Display(Name = "Unconscious")]
        Unconscious
    }
}