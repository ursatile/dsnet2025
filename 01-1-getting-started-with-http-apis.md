---
title: "1.1: Getting started with HTTP APIs"
layout: module
nav_order: 1.1
summary: >
   In this module, we'll create a set of simple HTTP API endpoints so we can work with vehicle data without having to go via the web interface.

typora-root-url: ./
typora-copy-images-to: ./assets\images
---

"Out of the box", Autobarn only exposes data via a web interface. To view or edit vehicle data, you've gotta use a web browser, and data is returned in HTML format -- which is cool for humans who like reading stuff, but if you're trying to build your own software to interact with Autobarn's data, HTML isn't a great format choice.

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
		private readonly IAutobarnDatabase db;

		public VehiclesController(IAutobarnDatabase db) {
			this.db = db;
		}

		// GET: api/vehicles
		[HttpGet]
		public IEnumerable<Vehicle> Get() => db.ListVehicles();
	}
}
```

There's a bunch of interesting stuff going on here - but the remarkable thing, really, is how much we've done with relatively few lines of code. 

The class is decorated with two attributes:

* `[ApiController]` wires up a bunch of default behaviour for binding parameters, like automatically converting JSON requests into .NET objects.
* `[Route("api/[controller]")]` makes the new controller available at `/api/vehicles`

We're using dependency injection to inject our `IAutobarnDatabase` into our class, and then creating a single method called `Get()`

All our method does is return `db.ListVehicles()`: ASP.NET will serialize the result into JSON for us and set the appropriate headers on the response.

### Interactive documentation with OpenAPI and Swagger

Now that we have an API method, let's make sure our users know about it - and know how to use it. We *could* write some documentation... but then we'd have to, y'know, *write documentation*. And publish it somewhere. And maintain it.

Instead, we're going to create an API spec using a format called OpenAPI, and then use a tool called Swagger to create live, interactive documentation based on our API spec. You can read more about using OpenAPI with .NET projects at [https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle](https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle)

First, we'll install the NuGet packages which will add OpenAPI support to our project:

```bash
cd Autobarn.Website
dotnet add package Swashbuckle.AspNetCore
```

Next, we'll add a line in `ConfigureServices` to enable SwaggerGen - the component which turns our .NET API code into a JSON file describing the API:

```
services.AddControllersWithViews().AddNewtonsoftJson();

// Add the Swagger generator to the services collection
services.AddSwaggerGen();

services.AddRazorPages().AddRazorRuntimeCompilation();
```

Finally, we need to enable the two required endpoints in our `Configure` method:

```csharp
app.UseAuthorization();

// Add endpoints for exposing the Swagger JSON document describing our API:
app.UseSwagger();
// ...and the SwaggerUI interactive API tooling.
app.UseSwaggerUI();

app.UseEndpoints(endpoints => {
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});

```

#### View your OpenAPI documentation spec:

Point a web browser at [https://localhost:5001/swagger/v1/swagger.json](https://localhost:5001/swagger/v1/swagger.json).

You should get a JSON document describing your API endpoints, and the various data types they accept and responses they return.

#### View your Swagger UI:

Point a browser at [https://localhost:5001/swagger/index.html](https://localhost:5001/swagger/index.html) - you should see the Swagger UI browser:

![image-20230521173726323](/assets/images/swagger-ui-example)

### API Description and Metadata

We'll specify some parameters when we call `AddSwaggerGen` to describe what our API is and what it does. We can also use these section to publish links to terms of service, contact information and license details:

```csharp
// Add the Swagger generator to the services collection
services.AddSwaggerGen(options => {
    options.SwaggerDoc("v1", new OpenApiInfo {
        Version = "v1",
        Title = "Autobarn API",
        Description = "The Autobarn vehicle platform API"
    });
});
```

## Adding More Controller Endpoints

First, we'll add a method to our controller which lets use an HTTP `POST` to add new vehicles to our database:

```csharp
// POST api/vehicles
[HttpPost]
public IActionResult Post([FromBody] VehicleDto dto) {
    var vehicleModel = db.FindModel(dto.ModelCode);
    var vehicle = new Vehicle {
        Registration = dto.Registration,
        Color = dto.Color,
        Year = dto.Year,
        VehicleModel = vehicleModel
    };
    db.CreateVehicle(vehicle);
    return Ok(vehicle);
}

```

Rebuild your solution, and you should see your new POST method in the SwaggerUI interface:

![image-20230521174602443](/assets/images/image-20230521174602443.png)

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

#### Supporting Newtonsoft.Json.JsonIgnore

Autobarn uses the Newtonsoft JSON serializer, which means that any properties in our model decorated with `[JsonIgnore]` are actually decorated with the Newtonsoft version of this attribute - and out of the box, `SwaggerGen` only respects the `System.Text.Json` version.

To get SwaggerGen to play nicely with Newtonsoft, we can install a package:

```bash
dotnet add package Swashbuckle.AspNetCore.Newtonsoft
```

and then add another line to our `ConfigureServices` method, *after* `.AddSwaggerGen()`:

```csharp
// explicit opt-in - needs to be placed after AddSwaggerGen().
services.AddSwaggerGenNewtonsoftSupport(); 
```

That'll tell SwaggerGen to ignore any model properties decorated with Newtonsoft's `JsonIgnore` attribute, and so we won't see docs or metadata generated for these properties and their underlying types.

### Exercise: Building HTTP APIs

Create a new controller at `Autobarn.Website/Controllers/Api/ModelsController.cs`

Implement HTTP endpoint methods for each of the following:

1. `GET /api/models` should return a list of all vehicle models in the system

2. `GET /api/models/{id}` should return the specific vehicle model identified by model code, e.g. `/api/models/dmc-delorean`. If no matching vehicle model exists, return `404`

3. `POST /api/models` should add a new model to the system. 

   * You will need to include the manufacturer name and model name in the request body.

   * What should you do if the manufacturer does not exist?









