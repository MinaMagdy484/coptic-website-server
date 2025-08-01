using System.Collections.Generic;

namespace DEPI_Project1.ViewModels
{
    public class UserAccountViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public IList<string> Roles { get; set; }
    }
}