using Code_Vanta_Systems.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Code_Vanta_Systems.Models;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LocationController : Controller
    {
        private readonly ILocationService _locationService;

        public LocationController(ILocationService locationService)
        {
            _locationService = locationService;
        }

        public async Task<IActionResult> Index()
        {
            var locations = await _locationService.GetAllAsync();
            return View(locations);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Location location)
        {
            if (ModelState.IsValid)
            {
                await _locationService.CreateAsync(location);
                return RedirectToAction(nameof(Index));
            }
            return View(location);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var location = await _locationService.GetByIdAsync(id);
            if (location == null) return NotFound();
            return View(location);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Location location)
        {
            if (ModelState.IsValid)
            {
                await _locationService.UpdateAsync(location);
                return RedirectToAction(nameof(Index));
            }
            return View(location);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var location = await _locationService.GetByIdAsync(id);
            if (location == null) return NotFound();
            return View(location);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _locationService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }

}
