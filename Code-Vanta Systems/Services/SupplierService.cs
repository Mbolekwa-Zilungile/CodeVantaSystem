using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ApplicationDbContext _context;

        public SupplierService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Supplier>> GetAllAsync()
        {
            return await _context.Suppliers
                .Include(s => s.Fridges)
                .Include(s => s.Quotations)
                .ToListAsync();
        }

        public async Task<Supplier> GetByIdAsync(int id)
        {
            return await _context.Suppliers
                .Include(s => s.Fridges)
                .Include(s => s.Quotations)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task CreateAsync(Supplier supplier)
        {
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Supplier supplier)
        {
            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
            }
        }
    }

}
