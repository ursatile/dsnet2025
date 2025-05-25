---
title: "2.4: REST Review and Recap"
layout: module
nav_order: 2.4
summary: >
    Review, recap, and further reading about designing REST APIs and hypermedia formats.
---

## REST: Review and Recap

* REST is an architectural style based on HTTP
* REST APIs include hypermedia links and actions in their responses, and use hypermedia to inform clients about what operations and resources are available given the current state of the application.
* We can use various features of C# and .NET, such as anonymous types and dynamic objects, to create hypermedia resources within our ASP.NET API actions.

## Examples of REST and Hypermedia APIs

Some examples of real-world organisations using hypermedia in their APIs. 

The **GitHub API** uses an HTTP `Link` header in many responses for pagination. 

[https://docs.github.com/en/rest/guides/traversing-with-pagination](https://docs.github.com/en/rest/guides/traversing-with-pagination)

**Spotify’s** API uses hypermedia extensively - for example, check out the structure of the `tracks` property that’s included when you retrieve a `playlist` resource:

[https://developer.spotify.com/documentation/web-api/reference/#/operations/get-playlist](https://developer.spotify.com/documentation/web-api/reference/#/operations/get-playlist)

The **Amazon API Gateway** service exposes a management API based on REST that uses HAL+JSON extensively. (Yes, that’s a REST API you use to configure REST APIs… it gets a bit meta). There’s a good example of this in the documentation for the `method-response` endpoint:

[https://docs.aws.amazon.com/apigateway/api-reference/resource/method-response/](https://docs.aws.amazon.com/apigateway/api-reference/resource/method-response/)

## Resources and Further Reading

REST is a far more complex topic than we have time to cover in this workshop; this section provides an overview and introduces the idea of using hypermedia to manage application state, but doesn't go into areas like API versioning, content negotiation, custom media types, and some of the more unusual parts of the HTTP protocol that come into play when we're designing hypermedia APIs.

### Dylan Beattie's "Real World REST" workshop

If you're interested in a more in-depth look at REST, I run a [two-day online workshop focusing on REST](https://ursatile.com/workshops/real-world-rest-with-csharp-and-dotnet.html), hypermedia APIs, and how you implement many of these patterns using C# and .NET. 

### Articles about REST:

**Roy Fielding: "Architectural Styles and the Design of Network-based Software Architectures"** ([https://www.ics.uci.edu/~fielding/pubs/dissertation/top.htm](https://www.ics.uci.edu/~fielding/pubs/dissertation/top.htm))

Roy Fielding created REST, and the definitive reference on the REST architectural style is his PhD thesis published in 2000. Elements of it are dated and have been superseded by newer patterns and techniques, but it's a valuable and insightful look at the way REST builds on the architectural principles of HTTP and the world wide web.

**Linking and Resource Expansion: REST API Tips** from StormPath

[https://stormpath.com/blog/linking-and-resource-expansion-rest-api-tips](https://stormpath.com/blog/linking-and-resource-expansion-rest-api-tips)

An article describing the [resource expansion pattern](https://stormpath.com/blog/linking-and-resource-expansion-rest-api-tips#resource-expansion) and linking strategies you can use to minimise the traffic required to work with your REST APIs.

### REST clients: 

* Postman: [https://www.postman.com/product/rest-client/](https://www.postman.com/product/rest-client/)
* Insomnia: [https://insomnia.rest/products/insomnia](https://insomnia.rest/products/insomnia)

### Hypermedia formats:

The examples in this workshop use HAL, mainly because it's relatively lightweight and easy to understand. Other hypermedia formats are available; here are the ones I'd suggest you look at before designing a hypermedia API for a production system:

I'd also recommend reading Kevin Sookocheff's article "[On choosing a hypermedia type for your API](https://sookocheff.com/post/api/on-choosing-a-hypermedia-format/)" includes a detailed comparison of many of these media types.

**HAL** links and resources:

* JSON Hypermedia API Language draft spec: [https://tools.ietf.org/id/draft-kelly-json-hal-02.html](https://tools.ietf.org/id/draft-kelly-json-hal-02.html)
* Mike Kelly: "Hypertext Application Language: A lean hypermedia type": [https://stateless.group/hal_specification.html](https://stateless.group/hal_specification.html)

**JSON for Linked Documents (JSON-LD)** is a hypermedia JSON format endorsed by the World Wide Web consortium. 

* [https://json-ld.org/](https://json-ld.org/)
* JSON-LD Best Practices: a W3C Working Group Note: [https://w3c.github.io/json-ld-bp/](https://w3c.github.io/json-ld-bp/)
* Steal Our JSON-LD: a site with snippets and examples for using JSON-LD to represent all sorts of objects and events. [https://jsonld.com/](https://jsonld.com/)

**HYDRA** is an extension to JSON-LD that supports hypermedia actions as well as links.

* [http://www.markus-lanthaler.com/hydra/](http://www.markus-lanthaler.com/hydra/)

**Collection+JSON** is a read/write hypermedia format based on JSON. 

* http://amundsen.com/media-types/collection/

**SIREN** is a format for adding hypermedia to generic entities, and includes an option `class` property that can be used to describe the type of resource being returned.

* https://github.com/kevinswiber/siren

----

