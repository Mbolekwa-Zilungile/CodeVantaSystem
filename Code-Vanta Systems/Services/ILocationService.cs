namespace Code_Vanta_Systems.Services
{
    using Code_Vanta_Systems.Models;

    public interface ILocationService
    {
        Task<IEnumerable<Location>> GetAllAsync();
        Task<Location> GetByIdAsync(int id);
        Task CreateAsync(Location location);
        Task UpdateAsync(Location location);
        Task DeleteAsync(int id);
    }

}
