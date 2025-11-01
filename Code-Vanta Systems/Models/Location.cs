using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using System.ComponentModel.DataAnnotations;

public class Location
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Description { get; set; }

    [Required, StringLength(200)]
    public string Address { get; set; }

    [Required, StringLength(200)]
    public string City { get; set; }

    [Required]
    public Region Province { get; set; }

    // Navigation properties - UPDATED:
    public virtual ICollection<Customer> Customers { get; set; }

    // CHANGE: Fridges → FridgeUnits ✅
    public virtual ICollection<FridgeUnit> FridgeUnits { get; set; }

    public virtual ICollection<FridgeRequest> FridgeRequests { get; set; }
    public virtual ICollection<Employee> Employees { get; set; }
    public virtual ICollection<TechnicianSchedule> ScheduledVisits { get; set; }

    public Location()
    {
        Customers = new HashSet<Customer>();
        FridgeUnits = new HashSet<FridgeUnit>(); // ✅ UPDATED
        FridgeRequests = new HashSet<FridgeRequest>();
        Employees = new HashSet<Employee>();
        ScheduledVisits = new HashSet<TechnicianSchedule>();
    }
}