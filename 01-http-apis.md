---
title: "1.1: Getting started with HTTP APIs"
layout: module
nav_order: 1.1
summary: >
   In this module, we'll create a set of simple HTTP API endpoints so we can work with vehicle data without having to go via the web interface.

typora-root-url: ./
typora-copy-images-to: ./assets\images
---

"Out of the box", Autobarn only exposes data via a web interface. To view or edit vehicle data, you have to use a web browser, and data is returned in HTML format -- which is cool for humans who like reading stuff, but if you're trying to build your own software to interact with Autobarn's data, HTML isn't a great format choice.

We're going to create an HTTP API so that folks out there on the internet can build their own software tools that connect to Autobarn.

### Our first API endpoint

The first endpoint we're going to create will return a list of every vehicle in the system.

Create a new folder in `Autobarn.Website\Controllers` called `Api`.

Add a new class in this folder called `VehiclesController.cs`

```csharp
using Autobarn.Data;
using Autobarn.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Autobarn.Website.Controllers.Api {
	[Route("api/[controller]")]
	[ApiController]
	public class VehiclesController : ControllerBase {
		private readonly AutobarnDbContext db;

		public VehiclesController(AutobarnDbContext db) {
			this.db = db;
		}

		// GET: api/vehicles
		[HttpGet]
		public IEnumerable<Vehicle> Get() => db.Vehicles.ToList();
	}
}
```

There's a bunch of interesting stuff going on here - but the remarkable thing, really, is how much we've done with relatively few lines of code.

The class is decorated with two attributes:

* `[ApiController]` wires up a bunch of default behaviour for binding parameters, like automatically converting JSON requests into .NET objects.
* `[Route("api/[controller]")]` makes the new controller available at `/api/vehicles`

We're using dependency injection to inject our `AutobarnDbContext` into our class, and then creating a single method called `Get()`

All our method does is return `db.Vehicles().ToList()`: ASP.NET will serialize the result into JSON for us and set the appropriate headers on the response.

## Adding More Controller Endpoints

First, we'll add a method to our controller which lets use an HTTP `POST` to add new vehicles to our database:

```csharp
// POST api/vehicles
[HttpPost]
public async Task<IActionResult> Post([FromBody] VehicleDto dto) {
    var vehicleModel = db.FindModel(dto.ModelCode);
    var vehicle = new Vehicle {
        Registration = dto.Registration,
        Color = dto.Color,
        Year = dto.Year,
        VehicleModel = vehicleModel
    };
    db.Vehicles.Add(vehicle);
    await db.SaveChangesAsync();
    return Ok(vehicle);
}
```

Next, we'll add a method that uses HTTP `PUT` to *create or update* a vehicle in the database.

> The semantics of HTTP PUT can be a little confusing. I tend to think of it as "here is a resource: PUT the resource at this address, whether it already exists or not."

```csharp
// PUT api/vehicles/ABC123
[HttpPut("{id}")]
public IActionResult Put(string id, [FromBody] VehicleDto dto) {
    var vehicleModel = db.FindModel(dto.ModelCode);
    var vehicle = new Vehicle {
        Registration = dto.Registration,
        Color = dto.Color,
        Year = dto.Year,
        ModelCode = vehicleModel.Code
    };
    db.UpdateVehicle(vehicle);
    return Ok(dto);
}

```

Finally, we'll add a method that uses HTTP `DELETE` to remove a vehicle from the database:

```csharp
// DELETE api/vehicles/ABC123
[HttpDelete("{id}")]
public IActionResult Delete(string id) {
    var vehicle = db.FindVehicle(id);
    if (vehicle == default) return NotFound();
    db.DeleteVehicle(vehicle);
    return NoContent();
}
```

# Exercise: Building HTTP APIs

Create a new controller at `Autobarn.Website/Controllers/Api/ModelsController.cs`

Implement HTTP endpoint methods for each of the following:

1. `GET /api/models` should return a list of all vehicle models in the system

2. `GET /api/models/{id}` should return the specific vehicle model identified by model code, e.g. `/api/models/dmc-delorean`. If no matching vehicle model exists, return `404`

3. `POST /api/models` should add a new model to the system.
* You will need to include the manufacturer name and model name in the request body.

* What should you do if the manufacturer does not exist?

# Exercise: Create an HTTP API client

The Autobarn project includes a simple HTTP API based on ASP.NET WebAPI, that supports the following methods:

**GET /api/vehicles/**

Returns a list of all the vehicles stored in the system

**POST /api/vehicles/**

Add a new vehicle to the system

**PUT /api/vehicles/{registration}**

Create or update the vehicle with the specified registration

**DELETE /api/vehicles/{registration}**

Delete the vehicle with the specified registration

**GET /api/models**

Returns a list of all the vehicle models available in the system

**GET /api/makes**

Returns a list of all the vehicle makes available in the database

## Requirements

Create a .NET console application that will add a randomly-generated vehicles to the Autobarn platform every time the user presses a key.

* The `modelCode` should be selected at random from the codes returned by the `GET /api/models` endpoint
* The list of model codes should be cached so it's only retrieved once, when your application first starts
* The `year` should be a random integer between 1960 and 2025
* The `color` should be one of the CSS named colours:
  * [https://developer.mozilla.org/en-US/docs/Web/CSS/named-color](https://developer.mozilla.org/en-US/docs/Web/CSS/named-color)
* The `registration` should be 8 randomly generated characters in the range A-Z 0-9.

## Hints

* Use the `System.Net.HttpClient` class to make HTTP requests and handle responses; you'll find lots of information about how to use this class at [https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient)

* Wrap the raw `HttpClient` in a wrapper class which exposes strongly-typed methods that abstract away the HTTP details in favour of business-level operations:
  ```csharp
  public class AutobarnApiClient(HttpClient http) {
  
  	public string[] ListModelCodes() {
          // TODO: list all model codes from GET /api/models
      }
  
      public VehicleDto CreateRandomVehicle() {
          // TODO: generate a random vehicle 
  	}
  }
  ```

* [`System.Random.Shared`](https://learn.microsoft.com/en-us/dotnet/api/system.random.shared?view=net-9.0) provides an instance of the `System.Random` random number generator you can use to generate random numbers; use the `.Next(10)` method to generate a random integer between 0 and 10

* Tools like ChatGPT are very good at handling requests like "list the named web colors as a .NET string array" 😉









