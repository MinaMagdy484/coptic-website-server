using DEPI_Project1.Models;
using System.ComponentModel.DataAnnotations; 
namespace DEPI_Project1.ViewModels
{
    public class RegisterViewModel
    {
        public string? UserType { get; set; }
        [Required]
        [RegularExpression(@"^[a -zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }

        public string Name { get; set; }
    }


}
