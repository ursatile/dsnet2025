---
title: "2.1: Introducing REST"
layout: module
nav_order: 2.1
summary: >
    Autobarn includes a simple HTTP API. In this module, we'll look at the difference between HTTP and REST, and introduce some RESTful principles to improve the design and behaviour of our API.
---

The Autobarn project includes a simple HTTP API based on ASP.NET WebAPI, that supports the following methods:

**GET /api/vehicles/**

Returns a list of all the vehicles stored in the system

**POST /api/vehicles/**

Add a new vehicle to the system

**PUT /api/vehicles/{registration}**

Create or update the vehicle with the specified registration

**DELETE /api/vehicles/{registration}**

Delete the vehicle with the specified registration

> ℹ You can send GET requests using a normal web browser, but you'll find working with HTTP APIs much easier if you install a development HTTP client such as [Postman](https://www.postman.com/product/rest-client/) or [Insomnia](https://insomnia.rest/products/insomnia). The screenshots in this section are from Postman.

### Pagination for large data sets

Take a look at the results of `GET /api/vehicles` on our API. It's a HUGE list of data; every single vehicle in our data set, in one enormous JSON array. We're transferring a lot of data that isn't really necessary – and if we want to consume that dataset through some sort of rich client, we're going to either need to retrieve all 5,000+ vehicles once and cache it locally, or we'll need to retrieve it every time, filter it on the client side to extract the records we need, and discard the rest. Neither of those is ideal.

One approach we could use here would be to paginate our data set: break it into pages, and retrieve each page one at a time:

```
GET /api/vehicles

// Defaults to page=0; assume 10 records per page; returns records 0-9

GET /api/vehicles?page=1

// returns records 10-19

GET /api/vehicles?page=2

// returns records 20-29
```

There are two problems with this approach:

1. The client needs to know how it works. Which means somebody has to read the documentation, which means we need to *write* the documentation, and maintain it, and… 
2. If we change our pagination strategy, all the clients will break. Say we decide that instead of breaking the list down by record number, we're going to break it down alphabetically; all the vehicles with a registration beginning `A` on one page (`/api/vehicles?group=a`), another page for all the vehicles beginning `B` (`/api/vehicles?group=b`), and so on. Requests with `?page=2` will then fail because they don't use our new pagination scheme.

### Introducing Hypermedia

There's a lot of discussion about what makes an API "properly RESTful" – and, to be completely honest, it's not that big a deal. Like any pattern or architectural style, REST won't magically solve all your problems – but if you are dealing with particular kinds of problems, applying some RESTful principles might make your life easier and avoid reinventing the wheel.

If I had to identify one fundamental difference between REST and regular HTTP APIs, it would be **hypermedia**. Just as the world wide web uses hyperlinks to connect related pages, a properly RESTful API should return resources that include hypermedia links that the client can use to explore other resources exposed by that API.

Above, we identified two problems with our pagination approach. To solve these problems, we're going to modify our API endpoint so that instead of returning a regular JSON array, we're going to return a hypermedia JSON object that uses **metadata** to include pagination links and context, as well as the actual data we've requested.

In these examples, we're using a JSON hypermedia format based on the Hypertext Application Language (HAL), created by Mike Kelly. The HAL specification is available at [https://datatracker.ietf.org/doc/html/draft-kelly-json-hal-08](https://datatracker.ietf.org/doc/html/draft-kelly-json-hal-08)

HAL is a superset of JSON – in other words, a HAL document is a valid JSON document that includes some extra fields and properties. The MIME type for HAL is `application/hal+json`; hypermedia API endpoints should return this in the `Content-Type` header, and clients that support hypermedia can check for this header to determine whether a response includes hypermedia metadata or not.

> ℹ The HAL draft spec expired in 2017, and has never been formalised, but that's not actually a problem. There hasn't been any real standardisation of hypermedia API clients the way we've seen with web browsers; there's nothing in the hypermedia API domain comparable to the way HTML defines a standard for hypermedia documents and most companies integrating with hypermedia APIs will develop bespoke clients.
>
> HAL is also a relatively lightweight format that looks similar to regular JSON, so it's ideal for workshops and teaching because it's relatively easy to see what's going on.

At the moment, when we `GET /api/vehicles/`, we get a response something like this:

```json
[
    {
        "vehicleModel": {
            "manufacturer": {
                "code": "nissan",
                "name": "NISSAN"
            },
            "code": "nissan-note",
            "manufacturerCode": "nissan",
            "name": "NOTE"
        },
        /* ... 5000 more vehicles here ... */
    }
]
```

We're going to modify our API endpoint so that when we `GET /api/vehicles`, we get a response like this:

```json
{
  "_links": {
      "self": { "href": "/api/vehicles/?index=20&count=10" },
      "next": { "href": "/api/vehicles?index=30&count=10" },
      "previous": { "href": "/api/vehicles?index=30&count=10" },
      "first": { "href": "/api/vehicles?index=0&count=10" },
      "next": { "href": "/api/vehicles?index=5000&count=10" }
  },
  "total": 5001,
  "count": 10,
  "index", 20,
  "items": [
    /* ... JSON array of vehicles here ... */
  ]
}
```

Now, we could do this by creating a .NET `Link` class, with a property called `href`, and then create an object called `LinkCollection` or something with `Link` properties called `self`, `next`, `previous`, etc.

There's another approach we can take, though. Our web API here is a boundary layer between our backend system and the rest of the web. Most backend systems in .NET apps are created using lots of .NET classes containing business data and validation, and that's good – using a strongly-typed domain model on our backend provides all kinds of benefits when it comes to performance, maintainability, type checking, and so on. But the rest of the web? The web doesn't have a type system. The web is just loads of strings flying around all over the planet. HTML, XML, JSON, all sorts of proprietary formats – and when those strings arrive where they're going, something is going to parse those strings and try to make sense of them.

Our web API is the last port of call before our data leaves the safety of our strongly-typed domain and escapes onto the wild wild web, and so it doesn't really make sense to use a strongly-typed domain model at this level of our application.

Instead, we're going to use one of my favourite features of C# - **anonymous types**. C# allows us to declare anonymous objects without specifying a type:

```c#
var someObject = new {
   customer = new {
      firstName = "Alice",
      lastName = "Aardvark"
   },
   total = 123.45m,
   createdAtUtc = DateTime.UtcNow
}
```

and the great thing about this is that if we push that object through a JSON serializer, here's what comes out the other side:

```json
{
  "customer": {
    "firstName": "Alice",
    "lastName": "Aardvark"
  },
  "total": 123.45,
  "createdAtUtc": "2021-06-24T10:51:58.7028846Z"
}
```

For working with hypermedia formats like HAL, I find the resemblance between the .NET code and the resulting JSON objects very useful; the C# code we're writing is as close as possible to the JSON we're going to return from our API.

#### Adding links to our API response

To include hypermedia links in our API response for `GET /api/vehicles`, find the `Get` method on `/Controllers//Api/VehiclesController.cs` and replace it with this:

```c#
[HttpGet]
[Produces("application/hal+json")]
public IActionResult Get(int index = 0, int count = 10) {
  var vehicles = db.ListVehicles().Skip(index).Take(count);
  var total = db.CountVehicles();
  var result = new {
    _links = new {
      self = new { href = $"/api/vehicles?index={index}&count={count}" },
      first = new { href = $"/api/vehicles?index=0&count={count}" },
      previous = new { href = $"/api/vehicles?index={index - count}&count={count}" },
      next = new { href = $"/api/vehicles?index={index + count}&count={count}" },
      final = new { href = $"/api/vehicles?index={total - (total % count)}&count={count}" }
    },
    index,
    count,
    total,
    items = vehicles,
  };
  return Ok(result);
}
```

Here's what's going on here:

* We've added the `[Produces("application/hal+json")]` attribute, so that the response will have the correct `Content-Type` header.
* The return type is now an `IActionResult`; this gives us more control over HTTP response codes.
* We're calling `db.ListVehicles()` and then using LINQ's `.Skip(index).Take(count)` to return a subset of the data.[^1] 
* We're creating an anonymous object called `result`, which includes our hypermedia paging, our `index`, `count`, and `total` fields, and the actual response (sometimes called the *payload*) in the `items` field.
* We're returning the whole thing by passing it into the built-in `Ok` method, which will automatically serialize it to JSON and return it as an HTTP `200 OK` response.

[^1]: This only really works if your underlying data store understands LINQ queries and can translate them efficiently. Entity Framework will translate the `.Skip().Take()` here into a `WHERE` clause that's passed to the underlying database, so we're using the power of SQL Server to find and return only the records we're interested in. If your data store's `GetVehicles` method always returns every record in the table, you're going to be retrieving 5,000 vehicles and then filtering them in memory, which isn't very efficient. 

The key thing to understand here is that the **names** of those links form part of our API contract. By publishing this API, we are guaranteeing that this resource will always include a `_links` collection, and if more results are available, the client can retrieve them by following `_links.next.href`. If we change `next` to `forward`, we've broken the contract and existing clients will stop working.

However, if we change our pagination strategy, we can modify our API response so that `next.href` reflects our new paging scheme, and existing clients will just work. This flexibility to change endpoint URIs without breaking client applications is one of the key advantages of REST, particularly on APIs that are intended to run in production for a long time. As Roy Fielding, the creator of REST, has explicitly stated:[^1]

> “REST is software design on the scale of decades: every detail is intended to promote software longevity and independent evolution. Many of the constraints are directly opposed to short-term efficiency.”

[^1]: [https://roy.gbiv.com/untangled/2008/rest-apis-must-be-hypertext-driven#comment-724](https://roy.gbiv.com/untangled/2008/rest-apis-must-be-hypertext-driven#comment-724)

#### HATEOAS: Hypermedia as the Engine of Application State

Although we've added hypermedia links to our response, our implementation at the moment still isn't quite right. If a client has retrieved the very first page of results, it doesn't make any sense to link to a "previous" page - there *is* no previous page. Likewise, if a client is already at the final page of results, it doesn't make any sense to offer a link to the "next" page – this would move them off the end of the collection and at best they'd get an empty response.

> ℹ You'll notice I'm using the words "previous" and "final" instead of "last". The word "last" in English is ambiguous… if you're halfway through a book and I say "go to the last page", do I mean the *previous* page (the last page that you read), or the *final* page (the last page in the book?) Avoiding this kind of ambiguous language can make a big difference when folks are writing clients that consume data from your APIs.

This is where we introduce the idea of **application state**. When a client is accessing a resource, there are multiple things that might affect what data they can see and what operations they're allowed to do. In this example, **state** is about pagination: which page of data is the client looking at? Can they move forwards from here? Can they move backwards? These are **state transitions**, and HATEOAS is all about using hypermedia to communicate which state transitions are valid given the current state of the application.

What we need to do is to only include links like `next` and `previous` if they're valid for the current resource – and this presents us with another challenge. In the previous section, we used an anonymous type to represent our `_links` collection - but anonymous types are still strongly-typed. They don't have a name, but their structure is fixed by the C# compiler, and we can't arbitrarily add or remove properties from an anonymous type.

To do this, we'll need to use another feature of modern .NET - the **dynamic** type. Dynamically typed objects aren't subject to the same compile-time type constraints as static types; we can declare them, add any fields or properties we like, and the compiler will happy do whatever we've asked for and then figure out at runtime whether it worked or not.

We're going to implement a helper method here called `Paginate`, which takes a root URL, plus the `index` of the current page, the `count` of items on each page, and the `total` number of items in the data store, and returns a `dynamic` object containing our hypermedia links:

```csharp
private dynamic Paginate(string url, int index, int count, int total) {
    dynamic links = new ExpandoObject();
	links.self = new { href = url };
	links.final = new { href = $"{url}?index={total - (total % count)}&count={count}" };
	links.first = new { href = $"{url}?index=0&count={count}" };
	if (index > 0) links.previous = new { href = $"{url}?index={index - count}&count={count}" };
	if (index + count < total) links.next = new { href = $"{url}?index={index + count}&count={count}" };
	return links;
}
```

Now we can use our helper method to add the `_links` collection to our hypermedia response:

```csharp
[HttpGet]
[Produces("application/hal+json")]
public IActionResult Get(int index = 0, int count = 10) {
    var items = db.ListVehicles().Skip(index).Take(count);
    var total = db.CountVehicles();
    var _links = Paginate("/api/vehicles", index, count, total);
    var result = new { _links, index, count, total, items };
    return Ok(result);
}
```

## Exercise: Implementing HATEAOS

In the worked examples above, we added paging to our API using hypermedia links, based on arbitrary page indexes and counts.

Replace this with a different pagination system based on the first character of the vehicle license plate. You’ll need to make some decisions about how to implement your paging solution:

* What should page “zero” be?
* If there are no vehicles matching a particularly character, should you return an empty page?

Remember that you should not modify any of the link **names** or **structure** exposed by your API; these links form part of your implied contract with your API clients and consumers and you don’t want to break those.

