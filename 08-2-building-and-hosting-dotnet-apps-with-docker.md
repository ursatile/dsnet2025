---
title: "8.2: Using Docker to build and host .NET applications"
layout: module
nav_order: 8.2
summary: >
    In this module, we'll look at Docker, and how you can use it to build and run your .NET applications and microservices
---

## Using Docker to build and host .NET applications

We can use Docker to compile our .NET applications, and create a Docker image that we can use to host an individual Docker application.

To do this, we’ll use two different Docker images provided by Microsoft which are available on the public Docker registry:

For .NET 6.0:

* `mcr.microsoft.com/dotnet/sdk:6.0`
* `mcr.microsoft.com/dotnet/aspnet:6.0`

> Don’t worry about the `aspnet` in the second image name: it’s a minimal container built with Linux and the .NET runtime that can host microservice applications and services as well as ASP.NET web applications.

For .NET 3.1, the images we’ll use are:

* `mcr.microsoft.com/dotnet/core/sdk:3.0`
* `mcr.microsoft.com/dotnet/core/aspnet:3.0`

The example here deals with .NET Core 3.1; for .NET 6, see Microsoft’s tutorial [on using Docker images for ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-6.0).

We need to create a `Dockerfile` that specifies how to build and run the `Autobarn.Notifier` application:

```
# To build the Docker image for Autobarn.Notifier,
# run docker build from the SOLUTION DIRECTORY (/dotnet)
# docker build -f Autobarn.Notifier/Dockerfile -t autobarn.notifier .
# 
# Then run the Notifier docker image using:
# docker run autobarn.notifier

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

COPY . ./
RUN dotnet restore
RUN dotnet build
RUN dotnet publish Autobarn.Notifier -c Release -o published

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/published ./
ENTRYPOINT ["dotnet", "Autobarn.Notifier.dll"]
```

**References:**

* Tutorial: Docker images for ASP.NET Core:

    [https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-6.0](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-6.0)

  

