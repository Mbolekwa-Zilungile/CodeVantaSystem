using Code_Vanta_Systems.Models;
using Microsoft.AspNetCore.Identity;

public class RoleManagementViewModel
{
    public List<ApplicationUser> Users { get; set; }
    public List<IdentityRole> Roles { get; set; }
    public Dictionary<string, IList<string>> UserRoles { get; set; }
}
