---
title: "2.3: Hypermedia Actions"
layout: module
nav_order: 2.3
summary: >
    In this module, we'll go beyond simple HTTP GET and look at how to implement support for hypermedia actions.
---

## Going Beyond GET: Hypermedia Actions

So far, we've only used hypermedia for `GET` methods, which should be "read-only"; in a well-designed API, an HTTP `GET` should *never* change the state of the system.

> ℹ Because web browsers don't properly support HTTP verbs like DELETE and PATCH, it's fairly common to see web interfaces that use regular HTML links for admin operations like deleting records. Years ago, I was maintaining a site that used an open-source CMS whose main admin dashboard listed all the articles in the system, along with Edit and Delete links for each article. A vulnerability meant the admin page ended up getting crawled by Googlebot, which followed all the links on the page, sending an HTTP GET request to every link's HREF – and consequently deleted every single article in the entire site. That was not a good day.

As well as linking to other resources, we can also use hypermedia to advertise actions like `PUT`, `POST` and `DELETE`, along with information about what these methods do. Out of the box, HAL doesn't support hypermedia actions (take a look at the section below on [hypermedia formats](#hypermediaformats) to learn about some formats which do) – but for now, we're going to extend the HAL format and define our own structure for including actions in a hypermedia response.

To interact with our API endpoint, the client needs to know which HTTP method that corresponds to a particular action, and the URL that exposes this behaviour

We may also want to include the content type that the URL endpoint accepts

We want to extend our `GET /api/vehicles/{registration}` endpoint to look something like this:

```json
{
    "registration": "OUTATIME",
    "modelCode": "dmc-delorean",
    "color": "Silver",
    "year": 1985,
    "_actions": {
        "update": {
            "href": "/api/vehicles/outatime",
            "method": "PUT",
            "accept": "application/json"
        },
        "delete": {
            "href": "/api/vehicles/outatime",
            "method": "DELETE"
        }
    }
    "_links": {
        "self": {
            "href": "/api/vehicles/outatime"
        },
        "vehicleModel": {
            "href": "/api/models/dmc-delorean"
        }
    }
}
```

As before, we can do this using the flexibility of the `dynamic` object; since we're already coercing our `Vehicle` object into a `dynamic`, we only need to add a few lines of code directly into our controller action:

```csharp
// GET api/vehicles/ABC123
[HttpGet("{id}")]
public IActionResult Get(string id) {
	var vehicle = db.FindVehicle(id);
	if (vehicle == default) return NotFound();
	var json = vehicle.ToDynamic();
	json._links = new {
		self = new {href = $"/api/vehicles/{id}"},
		vehicleModel = new {href = $"/api/models/{vehicle.ModelCode}"}
	};
	json._actions = new {
		update = new {
			method = "PUT",
			href = $"/api/vehicles/{id}",
			accept = "application/json"
		},
		delete = new {
			method = "DELETE",
			href = $"/api/vehicles/{id}"
		}
	};
	return Ok(json);
}
```

### Using resource references in actions

Our API supports a PUT method for creating or updating details of a vehicle – but what if we need to change a vehicle's model? What if somebody's accidentally listed a Volkswagen Beetle as a Ford Mondeo? 

In this scenario, we need to include the correct **resource identifier** in our `PUT` request, and follow the same conventions as we've used elsewhere:

```json
PUT /api/vehicles/ABC123
Content-Type: application/hal+json
{
    "registration": "ABC123",
    "color": "Green",
    "year": 1976,    
    "_links": {
        "vehicleModel": {
            "href": "/api/models/volkswagen-beetle"
        }
    }
}

HTTP 201 No Content
Location: /api/vehicles/ABC123
```

To support this, our `PUT` method needs to support the hypermedia properties we're including in our request body, so we'll need to modify our `PUT` action as follows:

```csharp
// PUT api/vehicles/ABC123
[HttpPut("{id}")]
public IActionResult Put(string id, [FromBody] dynamic dto) {
	var vehicleModelHref = dto._links.vehicleModel.href;
	var vehicleModelId = ModelsController.ParseModelId(vehicleModelHref);
	var vehicleModel = db.FindModel(vehicleModelId);
	var vehicle = new Vehicle {
		Registration = id,
		Color = dto.color,
		Year = dto.year,
		ModelCode = vehicleModel.Code
	};
	db.UpdateVehicle(vehicle);
	return Get(id);
}
```

To translate the `href` URI back into a model code, we're using a static method on the `Controllers.api.ModelsController`. In a larger application, you'd do this by instantiating a `TemplateMatcher` and using the application's routing table to parse the URL; for now, we're going to split the string and return the last fragment:

```
public static string ParseModelId(dynamic href) {
	var tokens = ((string) href).Split("/");
	return tokens.Last();
}
```

## Exercise: Hypermedia Actions

In the last section, we created a resource for a specific manufacturer and a resource for a specific manufacturer/model combination.

Add hypermedia actions to your API to support the following scenarios:

* `POST /api/manufacturers/{code}` - to add a new model to the listing for a particular manufacturer

Bonus credit:

* `POST /api/manufacturers/{code}/models/{code}` - to add a new vehicle to the listing for a specific vehicle make and model.

To do this, you’ll need to figure out how to bind multiple route parameters, so that you can specify the manufacturer code and the model code as URL elements and the vehicle details as a JSON object in the POST body.
