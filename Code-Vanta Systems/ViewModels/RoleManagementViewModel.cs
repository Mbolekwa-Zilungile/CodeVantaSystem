using Code_Vanta_Systems.Models;
using Microsoft.AspNetCore.Identity;

namespace Code_Vanta_Systems.ViewModels
{
    public class RoleManagementViewModel
    {
        public List<ApplicationUser> Users { get; set; }
        public List<IdentityRole> Roles { get; set; }

    }
}
