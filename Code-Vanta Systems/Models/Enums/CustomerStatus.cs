namespace Code_Vanta_Systems.Models.Enums
{
    public enum CustomerStatus
    {
        Pending = 0, //Customer has registered but not yet reviewed by a liaison.
        Verified = 1, //Liaison confirms the customer’s details are valid.
        Active = 2, //Customer is actively using services.
        Suspended = 3, //Customer account is temporarily disabled.
        Rejected = 4, //Liaison determines the registration is invalid or fraudulent.
        Inactive = 5  //Customer hasn’t interacted with the system for a long period
    }
}
