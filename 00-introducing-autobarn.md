---
title: "Getting Started"
layout: module
nav_order: 0.1
summary: >
    All the examples in this workshop are based on Autobarn, a fictional website for listing used cars for sale. In this module, we'll get the Autobarn project running locally, and take a look at the project structure to see how it all fits together.
---

All the examples in this workshop are based on Autobarn, a fictional website for listing used cars for sale. Autobarn is an ASP.NET web application that uses an in-memory data store, so you can download and run the Autobarn code without any external dependencies.

### Prerequisites

To complete the exercises in the workshop, you'll need the Autobarn source code and a .NET runtime. The [Autobarn repository](https://github.com/ursatile/autobarn) is hosted on GitHub. Get started by cloning the Autobarn repo to your local machine:

```bash
git clone git@github.com:ursatile/autobarn.git
cd autobarn
cd dotnet
dotnet build
dotnet test
dotnet run --project Autobarn.Website
```

### Running Autobarn with an in-memory data store

By default, Autobarn runs against an in-memory data store based on CSV files. This means it doesn't need a SQL database or any external dependencies, but any changes you make will be lost when you stop the application. *(For testing & training, this is normally fine, and this app isn't intended to run in production anyway.)*

### Running Autobarn with a SQL Server database

Autobarn also includes a data store based on SQL Server and Entity Framework. The SQL database is available as a Docker image, so you'll need Docker installed to use this.

To use the SQL Server data store:

1. Run the Autobarn Docker image:

   `docker run -p 1433:1433 -d ursatile/ursatile-workshops:autobarn-mssql2019-latest`

2. Edit `Autobarn.Website\appsettings.json` and change the `DatabaseMode`:

   ````
   "DatabaseMode": "sql",
   ````

3. Run the `Autobarn.Website` project

The `Autobarn.Website.Tests` project overrides the website configuration, so the tests will *always* run against the in-memory data store.

