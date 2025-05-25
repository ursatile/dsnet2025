---
title: "5: Remote Procedure Calls with gRPC"
layout: module
nav_order: 5
summary: >
    In this module, we'll add a gRPC client and server to our application, and see how we can use Protocol Buffers to implement high-performance remote procedure calls.
---

In this module, we're going to demonstrate how you can use gRPC to implement fast, lightweight **remote procedure calls** between components in a distributed system.

To demonstrate this, we're going to create a new ASP.NET web application which exposes a gRPC server endpoint

### Creating a gRPC server with ASP.NET

.NET includes a project template for creating a simple gRPC server; we're going to use this as the starting point for our example.

Create the server using:

```
dotnet new grpc -o Autobarn.PricingServer
```

That'll spin up a new ASP.NET web application, wire in the required dependencies for hosting gRPC, and create a simple example of a protocol definition. Take a look in `Protos/greet.proto`:

```protobuf
syntax = "proto3";

option csharp_namespace = "Autobarn.PricingServer";

package greet;

// The greeting service definition.
service Greeter {
  // Sends a greeting
  rpc SayHello (HelloRequest) returns (HelloReply);
}

// The request message containing the user's name.
message HelloRequest {
  string name = 1;
}

// The response message containing the greetings.
message HelloReply {
  string message = 1;
}
```

This is a protocol definition file, which defines a signature for our service, the request and reply types used by that service. When we build our solution, the gRPC protocol compiler will translate this `.proto` file into a set of C# classes that will be compiled into our project.

We're going to replace the `greet.proto` with a protocol definition we can use in our Autobarn example.

Start by renaming `greet.proto` to `price.proto`. You'll also need to edit `Autobarn.PricingServer.csproj`; find this section:

```xml
<ItemGroup>
  <Protobuf Include="Protos\greet.proto" GrpcServices="Server" />
</ItemGroup>
```

and replace it with:

```xml
<ItemGroup>
  <Protobuf Include="Protos\price.proto" GrpcServices="Server" />
</ItemGroup>
```

Now open `price.proto` and replace the contents with this:

```protobuf
syntax = "proto3";

option csharp_namespace = "Autobarn.PricingServer";

package price;

service Pricer {
  rpc GetPrice (PriceRequest) returns (PriceReply);
}

message PriceRequest {
  string modelCode = 2;
  string color = 3;
  int32 year = 4;
}

message PriceResponse {
  int32 price = 1;
  string currencyCode = 2;
}
```

Delete the file `Services\GreeterService.cs` and create a new file `Services\VehiclePricingService.cs`:

```csharp
// Autobarn.PricingServer/Services/VehiclePricingService.cs

using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Autobarn.PricingServer.Services {
	public class VehiclePricingService : Pricer.PricerBase {
		private readonly ILogger<VehiclePricingService> logger;
		public VehiclePricingService(ILogger<VehiclePricingService> logger) {
			this.logger = logger;
		}

		public override Task<PriceReply> GetPrice(PriceRequest request, ServerCallContext context) {
			//TODO: calculate prices properly!
			return Task.FromResult(new PriceReply {
				Price = 5000,
                CurrencyCode = "EUR"
			});
		}
	}
}
```

Open `Startup.cs`, find this line:

```csharp
endpoints.MapGrpcService<GreeterService>();
```

and replace it with:

```csharp
endpoints.MapGrpcService<VehiclePricingService>();
```

#### Overriding the default ports for an ASP.NET web application

We need to run our pricing server alongside our main website application, which means we'll need to configure Kestrel (the web server built into ASP.NET web apps) to listen on a different port. Open `Autobarn.PricingServer\Program.cs` and replace the `Program` class with this:

```csharp
public class Program {
	public static void Main(string[] args) {
		CreateHostBuilder(args).Build().Run();
	}

	private const int HTTP_PORT = 5002;
	private const int HTTPS_PORT = 5003;

	public static IHostBuilder CreateHostBuilder(string[] args) =>
		Host.CreateDefaultBuilder(args)
			.ConfigureWebHostDefaults(webBuilder => {
				webBuilder.ConfigureKestrel(options => {
					options.ListenAnyIP(HTTP_PORT, listener => listener.Protocols = HttpProtocols.Http1AndHttp2);
					options.ListenAnyIP(HTTPS_PORT, listener => {
						listener.Protocols = HttpProtocols.Http1AndHttp2;
						listener.UseHttps();
					});
				});
				webBuilder.UseStartup<Startup>();
			});
}
```

We can now run `Autobarn.Website` on port 5000/5001, and `Autobarn.PricingServer` on ports 5002/5003.

### Creating a gRPC client

We're now going to create a simple console application that connects to our gRPC server and demonstrates how to run remote procedures via gRPC.

Create a new .NET console application:

```
dotnet new console -o Autobarn.PricingClient
```

Add the following NuGet packages:

* Google.Protobuf
* Grpc.Net.Client
* Grpc.Tools

```
dotnet add Autobarn.PricingClient package Google.Protobuf
dotnet add Autobarn.PricingClient package Grpc.Net.Client
dotnet add Autobarn.PricingClient package Grpc.Tools
```

We also need to add a reference to the same `price.proto` file we used in our pricing server, but this time we need to specify that we want to generate a gRPC **client** when we build the project. Open `Autobarn.PricingClient.csproj` and add a new `ItemGroup` entry:

```xml
<ItemGroup>
  <Protobuf Include="..\Autobarn.PricingServer\Protos\price.proto" GrpcServices="Client" />
</ItemGroup>
```

Notice here how the file path we're specifying is a relative path referring to the `price.proto` file in the `PricingServer` project – this way we don't end up with two different copies of the same file, which isn't a problem until somebody edits one of them and not the other and suddenly our client and server aren't using the same protocol definition any more. (This is bad.)

Here's the simplest possible gRPC client application; paste this into `Program.cs`:

```csharp
using System;
using Autobarn.PricingServer;
using Grpc.Net.Client;

namespace Autobarn.PricingClient {
	class Program {
		static void Main(string[] args) {
			using var channel = GrpcChannel.ForAddress("https://localhost:5003");
			var grpcClient = new Pricer.PricerClient(channel);
			Console.WriteLine("Ready! Press any key to send a gRPC request (or Ctrl-C to quit):");
			while (true) {
				Console.ReadKey(true);
				var request = new PriceRequest {
					ModelCode = "volkwsagen-beetle",
					Color = "Green",
					Year = 1985
				};
				var reply = grpcClient.GetPrice(request);
				Console.WriteLine($"Price: {reply.Price}");
			}
		}
	}
}
```

Now, if you run the `PricingServer` first, and then start the `PricingClient`, you should see requests appear in the server logs, and the replies written to the console by the client.

