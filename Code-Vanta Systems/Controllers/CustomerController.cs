using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Admin,Customer Liaison")]
    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly ApplicationDbContext _context;

        public CustomerController(ICustomerService customerService, ApplicationDbContext context)
        {
            _customerService = customerService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _customerService.GetAllAsync();
            return View(customers);
        }

        public IActionResult Create()
        {
            ViewBag.Locations = new SelectList(_context.Locations, "Id", "Description");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                await _customerService.CreateAsync(customer);
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Locations = new SelectList(_context.Locations, "Id", "Description", customer.LocationId);
            return View(customer);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null) return NotFound();

            ViewBag.Locations = new SelectList(_context.Locations, "Id", "Description", customer.LocationId);
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Customer customer)
        {
            if (ModelState.IsValid)
            {
                await _customerService.UpdateAsync(customer);
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Locations = new SelectList(_context.Locations, "Id", "Description", customer.LocationId);
            return View(customer);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _customerService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }

}
