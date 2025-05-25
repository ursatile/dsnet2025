---
title: "4: Message Queues and Pub/Sub"
layout: module
nav_order: 4
summary: >
    In this module, we'll look at message queues and the publish/subscribe pattern, and add a simple message queue and handler to Autobarn.
---

In modules 1 and 2, we looked at two patterns for communicating between different parts of a software application; REST, based on resources and representations, and GraphQL, which is based on queries.

Although REST and GraphQL are distinctly different patterns, they both use HTTP as a transport mechanism and are both based on the idea of requests and responses: your client send a request, and then nothing happens until you receive a response.

In this module, we're going to look at message queues, which offer a different way for your components to communicate.

### Message Queuing Concepts

A **queue** is really just a buffer that can store messages.

#### Direct Messages

The simplest message queue is a 1:1 relationship between a **producer** and a **consumer**. What it offers us is **temporal decoupling** – a way to send a message *now* but not have to handle it until later.

For example: say you have a checkout page on your website. When a customer completes an order, you want to send them an email, but you also want to display the order confirmation page as quickly as possible – and sending an email could take time. Instead, your web app (the **producer**) can send a message to a queue, which is a very fast, lightweight operation, and then a different service (the **consumer**) can receive that message, turn it into an SMTP email, and send the email.

### Topics, Exchangers, Publishers and Subscribers

Direct messaging has its uses, but a far more powerful and flexible messaging pattern is **publish/subscribe**, often shorted to just **pub/sub.**

In a pub/sub model, the producer **publishes** a message to an exchange, and consumers can **subscribe** to receive a copy of each message sent to that exchange. The real power of pub/sub is that you can have multiple subscribers to the same type of message. In some cases, this will be multiple instances of the same service - good for scaling out to deal with heavy workloads (e.g. transcoding a lot of video files, or sending a *lot* of email messages.) In other cases, you can have multiple services subscribed to the same message: when a customer places an order, one service might allocate orders in a warehouse, another one might raise an invoice, and a third might send a customer acknowledgement email.

## Adding pub/sub to Autobarn

In this exercise, we're going to add a simple publish/subscribe capability to Autobarn. Imagine that we have to provide a list to the government at the end of every month, in CSV format, with details of every vehicle that's been advertised for sale on our platform.

* When a new vehicle is added to the database, we want to **publish** a message containing details of that vehicle.
* We're going to create a **subscriber** component called the `Autobarn.AuditLog`. This component will subscribe to messages about new vehicles; every time a new vehicle is added to the system, the `AuditLog` will write the details of that vehicle to the end of a CSV log file.

For these examples, we're going to use a messaging library called [EasyNetQ](http://easynetq.com/), which provides some powerful and lightweight abstractions on top of RabbitMQ.

These examples will also require a RabbitMQ server. For local development, I'd recommend running RabbitMQ using Docker (see below). If you want to communicate across the internet with other machines, you can either run a Docker image in the cloud, or use a hosted service like [CloudAMQP](https://www.cloudamqp.com/).

> Most cloud providers now offer a message queueing platform. Microsoft Azure includes [Azure Service Bus](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview), AWS provides the [Amazon Simple Queue Service](https://aws.amazon.com/sqs/), and Google has the [Cloud Pub/Sub](https://cloud.google.com/pubsub) platform. They all have their own terminology and quirks, but the messaging patterns we're learning about can be implemented on top of any platform. There are also several mature open source .NET messaging frameworks, including [NServiceBus](https://particular.net/nservicebus) and [MassTransit](https://masstransit-project.com/), which run on top of various different transport layers so that you could, for example, use RabbitMQ for local development and then switch to Azure Service Bus for production deployments.

### Creating the Autobarn.AuditLog service

We're going to host our AuditLog service using the [.NET Generic Host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host?tabs=appbuilder), a base class which provides dependency injection, async startup/shutdown support, logging, and more. 

If you're using the `dotnet` CLI, run the following commands from the `dotnet` folder in the Autobarn project:

```bash
dotnet new console -o Autobarn.AuditLog
dotnet sln add Autobarn.AuditLog
```

If you're using Visual Studio, right-click the Autobarn solution in the Solution Explorer, Add, New Project, and add a new .NET Console application.

We'll need to install some NuGet packages - one for EasyNetQ itself, one which configures EasyNetQ to use the System.Text.Json serializer, and one for [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting) which includes the generic host:

```powershell
cd Autobarn.AuditLog
dotnet add package EasyNetQ
dotnet add package EasyNetQ.Serialization.SystemTextJson
dotnet add package Microsoft.Extensions.Hosting
```

#### Creating the Autobarn.Messages assembly

We're also going to create a .NET class library that contains the messages we're going to pass between our components:

```bash
dotnet new classlib -o Autobarn.Messages
dotnet sln add Autobarn.Messages
```

We need to add references to this new assembly to both our `AuditLog` and our `Website` projects:

```
dotnet add Autobarn.Website reference Autobarn.Messages
dotnet add Autobarn.AuditLog reference Autoarn.Messages
```

Next, replace `Class1.cs` with `NewVehicleMessage.cs`:

```csharp
// Autobarn.Messages/NewVehicleMessage.cs
using System;

namespace Autobarn.Messages {
	public class NewVehicleMessage {
		public string Registration { get; set; }
		public string Manufacturer { get; set; }
		public string ModelName { get; set; }
        public string ModelCode { get; set; }
		public string Color { get; set; }
		public int Year { get; set; }
		public DateTime ListedAtUtc { get; set; }
	}
}
```

> ℹ You might notice that we've flattened some of the object hierarchy - instead of `Manufacturer` and `Model` being object references, we're passing them as strings here. When you're designing messages, you should try to include enough information in each message that the receiver can handle that message without having to refer back to a central database or API. By doing this, you create a flexible architecture that allows you to spin up more receivers to cope with heavy traffic, without the risk of overloading the central database.

### Adding a message handler to Autobarn.AuditLog

We're going to add a simple handler to our AuditLog service. The service will connect to our RabbitMQ exchange, and subscribe to `NewVehicleMessage` messages; when a message is received, we'll run a handler that prints that message contents to the console.

```csharp
// Autobarn.AuditLog/Program.cs

using Autobarn.Messages;
using EasyNetQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var amqp = builder.Configuration.GetConnectionString("AutobarnRabbitMQ");
var bus = RabbitHutch.CreateBus(amqp, services => services.EnableSystemTextJson());
builder.Services.AddSingleton(bus);
builder.Services.AddHostedService<AuditLogService>();

var host = builder.Build();
await host.RunAsync();

```

We'll create the AuditLogService class which implements `IHostedService`:

```csharp
public class AuditLogService(IConfiguration config, IBus bus) : IHostedService {
	private const string SUBSCRIBER_ID = "Autobarn.AuditLog";

	public async Task StartAsync(CancellationToken token) {
		Console.WriteLine("Connected! Listening for NewVehicleMessage messages.");
		await bus.PubSub.SubscribeAsync<NewVehicleMessage>(SUBSCRIBER_ID, HandleNewVehicleMessage, token);
	}

	public Task StopAsync(CancellationToken cancellationToken) {
		Console.WriteLine("Shutting down Audit Log service.");
		return Task.CompletedTask;
	}

	private static void HandleNewVehicleMessage(NewVehicleMessage message) {
		var csv =
			$"{message.Registration},{message.Manufacturer},{message.ModelName},{message.Color},{message.Year},{message.ListedAtUtc:O}";
		Console.WriteLine(csv);
	}
}
```

and we'll add an `appsettings.json` file to our `AuditLog` app:

```json
{
  "ConnectionStrings": {
    "AutobarnRabbitMQ": "amqp://user:pass@localhost:5672"
  }
}
```

> ⚠ You need to set "Copy to Output Directory" for appsettings.json to "Copy if newer" in your project properties, so that your config file will be copied to the output directory along with your binaries.

You should now be able to run the `Autobarn.AuditLog` program, but it won't do anything yet, because nothing is publishing any `NewVehicleMessage` messages for it to handle.

### Adding a publisher to Autobarn.Website

In this section, we'll hook the`Autobarn.Website` project up to our message bus, and add some code to publish a` NewVehicleMessage` when a new vehicle is added to the platform.

We need to register `IBus` as a dependency, so add the following lines to the `ConfigureServices()` method in `Startup.cs`:

```csharp
var bus = RabbitHutch.CreateBus(Configuration.GetConnectionString("AutobarnRabbitMQ"));
services.AddSingleton<IBus>(bus);
```

You'll also need to add the RabbitMQ connection string to `appsettings.json`:

```diff
"ConnectionStrings": {
    "AutobarnSqlConnectionString": "Server=localhost;Database=Autobarn;User=autobarn;Password=autobarn;",
+    "AutobarnRabbitMQ":  "amqp://user:pass@localhost:5672"
  },
```

Next, we'll register `IBus` as a dependency of our API's `VehiclesController` method:

```csharp
namespace Autobarn.Website.Controllers.api {
	[Route("api/[controller]")]
	[ApiController]
	public class VehiclesController : ControllerBase {
		private readonly AutobarnDbContext db;
		private readonly IBus bus;

		public VehiclesController(AutobarnDbContext db, IBus bus) {
			this.db = db;
			this.bus = bus;
		}
        /*... rest of VehiclesController.cs ...*/
    }
}
```

We're going to add a private method called `PublishNewVehicleMessage`, which will take a `Vehicle`, create a `NewVehicleMessage` and publish it onto our bus:

```csharp
private void PublishNewVehicleMessage(Vehicle vehicle) {
	var message = new NewVehicleMessage() {
		Registration = vehicle.Registration,
		Manufacturer = vehicle.VehicleModel?.Manufacturer?.Name,
		ModelName = vehicle.VehicleModel?.Name,
        ModelCode = vehicle.VehicleModel?.Code,
		Color = vehicle.Color,
		Year = vehicle.Year,
		ListedAtUtc = DateTime.UtcNow
	};
	bus.PubSub.Publish(message);
}
```

Finally, we need to add a line to our `Post` method to call `PublishNewVehicleMessage`:

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
	PublishNewVehicleMessage(vehicle);
	return Ok(dto);
}
```

Now, start both the `Autobarn.Website` and `Autobarn.AuditLog` applications, and send a `POST` request to `/api/vehicles`, and watch the console output from `Autobarn.AuditLog`:

![image-20210703145130983](assets/images/image-20210703145130983.png)



## Exercise

There are two other endpoints in our application that users can use to add new cars to the platform - there's a web form available in the front-end web app, and the `PUT /api/vehicles` endpoint will also allow a user to add a new vehicle.

Modify each of these methods to publish a `NewVehicleMessage` when a new vehicle is added to the platform.

> ⚠ For the `PUT` endpoint, you'll need to figure out how to distinguish between a new vehicle being added to the platform for the first time, and an update to an existing vehicle.

