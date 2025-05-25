using Autobarn.Data.Entities;
using Autobarn.Website.Controllers;
using Autobarn.Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Autobarn.Tests;

public class VehiclesControllerTests {
	[Fact]
	public async Task Vehicles_Index_Lists_Vehicles() {
		var db = TestDbContext.Create();
		var c = new VehiclesController(db);
		(await c.Index()).ShouldBeOfType<ViewResult>();
	}

	[Fact]
	public async Task Advertise_Vehicle_Advertises_Vehicle() {
		var db = TestDbContext.Create();
		var c = new VehiclesController(db);
		var post = new VehicleDto {
			ModelCode = "volkswagen-beetle",
			Registration = "BUG1234",
			Color = "Yellow",
			Year = 1988
		};
		var result = await c.Advertise(post);
		result.ShouldBeOfType<RedirectToActionResult>();

		var db2 = TestDbContext.Connect(db.GetSqliteDbName());
		var created = await db2.Vehicles.FirstOrDefaultAsync(d => d.Registration == post.Registration);
		created.ShouldBeOfType<Vehicle>();
	}
}
