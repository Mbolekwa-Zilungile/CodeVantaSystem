using System.ComponentModel.DataAnnotations;

namespace Code_Vanta_Systems.Models.Enums
{
    public enum Region
    {
        [Display(Name = "Gauteng")]
        Gauteng = 1,

        [Display(Name = "KwaZulu-Natal")]
        KwaZuluNatal = 2,

        [Display(Name = "Western Cape")]
        WesternCape = 3,

        [Display(Name = "Eastern Cape")]
        EasternCape = 4,

        [Display(Name = "Free State")]
        FreeState = 5,

        [Display(Name = "Limpopo")]
        Limpopo = 6,

        [Display(Name = "Mpumalanga")]
        Mpumalanga = 7,

        [Display(Name = "North West")]
        NorthWest = 8,

        [Display(Name = "Northern Cape")]
        NorthernCape = 9
    }
}
