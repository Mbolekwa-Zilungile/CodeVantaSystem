namespace Code_Vanta_Systems.ViewModels
{
    public class FridgeAllocationDisplayViewModel
    { 
            public string FridgeModel { get; set; }
            public string ImageUrl { get; set; }
            public int Quantity { get; set; }
            public DateTime AllocatedOn { get; set; }
            public string Source { get; set; }    
            public int? RentalId { get; set; }   
    }
}
