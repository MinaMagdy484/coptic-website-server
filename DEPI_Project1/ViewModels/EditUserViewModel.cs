using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DEPI_Project1.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string UserName { get; set; }

        public IList<string> CurrentRoles { get; set; }
    }
}