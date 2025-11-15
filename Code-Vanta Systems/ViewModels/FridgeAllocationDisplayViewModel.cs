namespace Code_Vanta_Systems.ViewModels
{
    public class FridgeAllocationDisplayViewModel
    {
            // This holds the ID of the general Fridge model, used for original linking.
            public int FridgeModelId { get; set; }

            // CRITICAL FIX: Add the FridgeUnitId to link to specific physical unit history/faults.
            public int UnitId { get; set; }
            // CRITICAL FIX: Add SerialNumber for display
            public string SerialNumber { get; set; }

            // CRITICAL FIX: Add Condition for display (use string to avoid enum referencing complexity in view)
            public string Condition { get; set; }
            public string FridgeModel { get; set; }
            public string ImageUrl { get; set; }
         
            public DateTime AllocatedOn { get; set; }
            public string Source { get; set; }    
            public int? RentalId { get; set; }   
    }
}
