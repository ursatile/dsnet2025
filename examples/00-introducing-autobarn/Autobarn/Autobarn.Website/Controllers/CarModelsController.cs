using Autobarn.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Autobarn.Website.Controllers;

public class CarModelsController(AutobarnDbContext db) : Controller {
	public async Task<IActionResult> Index() {
		var list = await db.Models
			.Include(m => m.Make)
			.ToListAsync();
		return View(list);
	}

	public async Task<IActionResult> Details(string id) {
		var make = await db.Models
			.Include(m => m.Make)
			.Include(m => m.Vehicles)
			.FirstOrDefaultAsync(m => m.Code == id);
		return View(make);
	}
}
