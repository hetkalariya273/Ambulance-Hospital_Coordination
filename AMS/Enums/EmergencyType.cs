using System.ComponentModel.DataAnnotations;


namespace AMS.Enums
{
    public enum EmergencyType
    {
        [Display(Name = "Accident")]
        Accident,

        [Display(Name = "Heart Attack")]
        HeartAttack,

        [Display(Name = "Stroke")]
        Stroke,

        [Display(Name = "Pregnancy")]
        Pregnancy,

        [Display(Name = "Trauma")]
        Trauma,

        [Display(Name = "Fire Injury")]
        FireInjury,

        [Display(Name = "Breathing Problem")]
        BreathingProblem,

        [Display(Name = "Other")]
        Other
    }
}
