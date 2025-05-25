---
title: "2.2: Hypermedia Resources"
layout: module
nav_order: 2.2
summary: >
    In this module, we'll see how to convert plain old C# / JSON objects into rich hypermedia resources.
---

## Hypermedia Resources

We're now going to look at the `GET /api/vehicles/{registration}` endpoint, which returns details of a specific vehicle. Currently, this method returns a JSON representation of the requested vehicle (or `404 Not Found` if no such vehicle exists):

```json
GET /api/vehicles/outatime

{
    "vehicleModel": {
        "manufacturer": {
            "code": "dmc",
            "name": "DMC"
        },
        "code": "dmc-delorean",
        "manufacturerCode": "dmc",
        "name": "DELOREAN"
    },
    "registration": "OUTATIME",
    "modelCode": "dmc-delorean",
    "color": "Silver",
    "year": 1985
}
```

By default, ASP.NET is serializing the entire object graph here, so the `vehicleModel` and the `vehicleModel.manufacturer` objects are being encoded and included in our API response. For complex object models, this could result in a lot of unnecessary information being included in every API response, so instead of encoding the data inline, we're going to use hypermedia to handle these kinds of associations.

As with the previous example, we're going to use the flexibility of the `dynamic` type to transform our strongly-typed .NET objects into hypermedia resources. We're going to introduce another helper method here:

```csharp
public static class HypermediaExtensions {
    public static dynamic ToDynamic(this object value) {
		IDictionary<string, object> expando = new ExpandoObject();
		var properties = TypeDescriptor.GetProperties(value.GetType());
		foreach (PropertyDescriptor property in properties) {
		    expando.Add(property.Name, property.GetValue(value));
    	}
		return (ExpandoObject)expando;
    }
}
```

There's a lot of interesting .NET stuff going on in this method, so we'll break it down and look at it line by line.

```csharp
public static dynamic ToDynamic(this object value) {
```

 This defines an extension method on `object`, which is the fundamental base class of the entire .NET type system. Everything in .NET derives from `object`, so an extension method on `object` will be available on any object anywhere in our application. 

```csharp
IDictionary<string, object> expando = new ExpandoObject();
```

There are three different ways to interact with `dynamic` types in .NET. One is to use the C# `dynamic` keyword directly, but when we declare an object as `dynamic`, .NET actually instantiates an `ExpandoObject` – and we can manipulate the properties of this object via the `IDictionary<string, object>` interface. 

Next, we're going to use reflection to obtain a **type descriptor** of the object we're working with, so we can map the property names and values from our source object onto our new dynamic object. Notice that we checking each property to see if it's decorated with the `[JsonIgnore]` attribute, and skip any properties that are; this ensures that properties marked as `[JsonIgnore]` don't get included in our `dynamic` object:

```csharp
var properties = TypeDescriptor.GetProperties(value.GetType());
foreach (PropertyDescriptor property in properties) {
	expando.Add(property.Name, property.GetValue(value));
}
```

Finally, we cast our `IDictionary<string,object>` back to an `ExpandoObject` and return it:

```csharp
return (ExpandoObject)expando;
```

Once we've added our `ToDynamic` method to our codebase, we can use it in any of our controller actions:

```csharp
[HttpGet("{id}")]
[Produces("application/hal+json")]
public IActionResult Get(string id) {
	var vehicle = db.Vehicles.FirstOrDefaultAsync(v => v.Registration == id);
	if (vehicle == default) return NotFound();
	var json = vehicle.ToDynamic();
	return Ok(json);
}
```

We need to make a few more tweaks, though. Here's what we'll get back if we run this code:

```json
{
    "VehicleModel": {
        "manufacturer": {
            "code": "dmc",
            "name": "DMC"
        },
        "code": "dmc-delorean",
        "manufacturerCode": "dmc",
        "name": "DELOREAN"
    },
    "LazyLoader": {},
    "Registration": "OUTATIME",
    "ModelCode": "dmc-delorean",
    "Color": "Silver",
    "Year": 1985
}
```

First, we want to exclude the `VehicleModel` property – we're going to use hypermedia references instead of returning this inline. To do this, we're going to decorate the `VehicleModel` property on our `Vehicle` object with the `[JsonIgnore]` attribute, and then modify our `ToDynamic` implementation to exclude any properties which have this attribute:

```csharp
using Newtonsoft.Json; // see note below about namespaces!

namespace Autobarn.Data.Entities {
	public partial class Vehicle {
		...
		[JsonIgnore]
		public virtual Model VehicleModel { get; set; }
	}
}
```

> ℹ Watch out for namespaces here. There's a `System.Text.Json.Serialization.JsonIgnoreAttribute` and a `Newtonsoft.Json.JsonIgnoreAttribute` - you'll need to use the attribute from whichever JSON serializer you're using in your project. Autobarn uses the `Newtonsoft.Json` serializer, so make sure add `using Newtonsoft.Json` directive - and be careful using tools like ReSharper that will automatically reference missing namespaces for you, in case they pick the wrong one.

Now we'll modify our `ToDynamic` method to filter out these attributes:

```csharp
public static class HypermediaExtensions {
	public static dynamic ToDynamic(this object value) {
		IDictionary<string, object> expando = new ExpandoObject();
		var properties = TypeDescriptor.GetProperties(value.GetType());
		foreach (PropertyDescriptor property in properties) {
			if (Ignore(property)) continue;
			expando.Add(property.Name, property.GetValue(value));
		}
		return (ExpandoObject)expando;
	}

    private static bool Ignore(PropertyDescriptor property) {
		return property.Attributes.OfType<Newtonsoft.Json.JsonIgnoreAttribute>().Any();
	}
}
```

We're going to make two more improvements here. First, if you're using the Entity Framework data provider, you'll notice a property called `LazyLoader` appearing on JSON objects. This is part of Entity Framework's internal plumbing (it's used to enable lazy loading of our database entities), and we don't really want it appearing on our API responses, so we'll add a line to the `HyperMediaExtensions.Ignore` method to filter this property out:

```csharp
private static bool Ignore(PropertyDescriptor property) {
	if (property.Name == "LazyLoader") return(true);
	return property.Attributes.OfType<Newtonsoft.Json.JsonIgnoreAttribute>().Any();
}
```

Finally, the JSON serializer creates camel-case property values by default, so when we serialize `SomeProperty` it'll come out as `someProperty`, but this doesn't apply to properties which are dictionary keys – and because `dynamic` objects are serialized via their `IDictionary` interface, we need to explicitly enable camel-casing for these property names.

Find the line in `Startup.cs` where we add `NewtonsoftJson` to the project:

```csharp
services.AddControllersWithViews().AddNewtonsoftJson();
```

The `AddNewtonsoftJson()` method takes an optional `Action<MvcNewtonsoftJsonOptions>` parameter, which we can use to override the default serialization support by setting `processDictionaryKeys` to `true`:

```csharp
services
	.AddControllersWithViews()
	.AddNewtonsoftJson(options => options.UseCamelCasing(processDictionaryKeys: true));
```

That's it - when we hit our `GET /api/vehicles/outatime` endpoint now, we'll get back a nice, clean JSON representation:

```json
{
    "registration": "OUTATIME",
    "modelCode": "dmc-delorean",
    "color": "Silver",
    "year": 1985
}
```

Now, we're going to add some hypermedia properties to include the resource's own URL (via the `_links.self.href` property) and a link to the vehicle model:

```csharp
[HttpGet("{id}")]
public IActionResult Get(string id) {
	var vehicle = db.FindVehicle(id);
	if (vehicle == default) return NotFound();
	var json = vehicle.ToDynamic();
	json._links = new {
		self = new { href = $"/api/vehicles/{id}" },
		vehicleModel = new {href = $"/api/manufacturers/{vehicle.Model.Manufacturer.Code}/models/{vehicle.Model.Code}"}
	};
	return Ok(json);
}
```

When we hit that endpoint now, we get this – a JSON resource representing a vehicle, including hypermedia links to related resources.

```json
{
    "registration": "OUTATIME",
    "modelCode": "dmc-delorean",
    "color": "Silver",
    "year": 1985,
    "_links": {
        "self": {
            "href": "/api/vehicles/outatime"
        },
        "model": {
            "href": "/api/manufacturers/dmc/models/delorean"
        }
    }
}	
```

#### Exercise: Hypermedia Resources

In this exercise, we’ll create a set of resource endpoints that return information about vehicle manufacturers and models

* Create an endpoint at `/api/manufacturers` that returns a list of vehicle manufacturers.

* Create an endpoint at `/api/manufacturers/{code}` that returns details of a specific manufacturer

Each manufacturer should include hypermedia links for `self` and `models`.

Exercise Part 2

Create an endpoint  at `/api/models/{code}/vehicles` which lists all the vehicles listed for sale matching a specified manufacturer and model.

There is already an endpoint at `/api/models/{code}` which returns information about a particular vehicle model. Extend this resource to include hypermedia links for `self`, `manufacturer`, and `vehicles`. The `manufacturer` and `vehicles` links should point to the endpoints you created in this exercise.

