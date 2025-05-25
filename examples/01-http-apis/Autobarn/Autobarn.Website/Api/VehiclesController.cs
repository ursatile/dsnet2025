using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Autobarn.Data;
using Autobarn.Data.Entities;

namespace Autobarn.Website.Api {
	[Route("api/[controller]")]
	[ApiController]
	public class VehiclesController(AutobarnDbContext db) : ControllerBase {

		[HttpGet]
		public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicles() {
			return await db.Vehicles.ToListAsync();
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<Vehicle>> GetVehicle(string id) {
			var vehicle = await db.Vehicles.FindAsync(id);
			if (vehicle == null) return NotFound();
			return vehicle;
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> PutVehicle(string id, Vehicle vehicle) {
			if (id != vehicle.Registration) {
				return BadRequest();
			}

			db.Entry(vehicle).State = EntityState.Modified;

			try {
				await db.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException) {
				if (!VehicleExists(id)) {
					return NotFound();
				} else {
					throw;
				}
			}

			return NoContent();
		}

		[HttpPost]
		public async Task<ActionResult<Vehicle>> PostVehicle(Vehicle vehicle) {
			db.Vehicles.Add(vehicle);
			try {
				await db.SaveChangesAsync();
			}
			catch (DbUpdateException) {
				if (VehicleExists(vehicle.Registration)) {
					return Conflict();
				} else {
					throw;
				}
			}

			return CreatedAtAction("GetVehicle", new { id = vehicle.Registration }, vehicle);
		}

		// DELETE: api/Vehicles/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteVehicle(string id) {
			var vehicle = await db.Vehicles.FindAsync(id);
			if (vehicle == null) {
				return NotFound();
			}

			db.Vehicles.Remove(vehicle);
			await db.SaveChangesAsync();

			return NoContent();
		}

		private bool VehicleExists(string id) {
			return db.Vehicles.Any(e => e.Registration == id);
		}
	}
}
