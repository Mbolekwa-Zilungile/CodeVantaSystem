namespace Code_Vanta_Systems.ViewModels.InventoryLiaison
{
    // Assuming this simple inheritance from the Create model provided previously
    // and adding the Id and ExistingImageUrl properties.
    public class EditFridgeModelViewModel : CreateFridgeModelViewModel
    {
        public int Id { get; set; }

        public string? ExistingImageUrl { get; set; }
    }
}