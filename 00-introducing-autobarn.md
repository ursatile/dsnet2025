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
dotnet run --project Autobarn.Website**
```

### Sqlite and in-memory databases

Autobarn uses Entity Framework (EF) Core, but it's configured to use the Sqlite DB provider with an in-memory database

> SQLite is a C-language library that implements a [small](https://sqlite.org/footprint.html), [fast](https://sqlite.org/fasterthanfs.html), [self-contained](https://sqlite.org/selfcontained.html), [high-reliability](https://sqlite.org/hirely.html), [full-featured](https://sqlite.org/fullsql.html), SQL database engine. SQLite is the [most used](https://sqlite.org/mostdeployed.html) database engine in the world. SQLite is built into all mobile phones and most computers and comes bundled inside countless other applications that people use every day.
>
> *from [https://sqlite.org/](https://sqlite.org/)*

In other words, SQLite is a tiny database engine that doesn't use a server: it runs in the same process as our application code, which makes it perfect for development and testing scenarios where we need to connect a database but we don't *really* care what happens to the data.

If you take a look in `Program.cs` you'll see these lines:

```csharp
SqliteConnection sqliteConnection = new($"Data Source=:memory:");
sqliteConnection.Open();
builder.Services.AddDbContext<AutobarnDbContext>(options => options.UseSqlite(sqliteConnection));
```

and

```
using var scope = app.Services.CreateScope();
await using var db = scope.ServiceProvider.GetRequiredService<AutobarnDbContext>();
db.Database.EnsureCreated();
```

The in-memory provider for Sqlite (initialised by the `"Data Source=:memory"` connection string) will create a temporary database that persists for as long as there's at least one open connection, so we explicitly open a connection that stays open for the lifetime of the application, and then we use EF Core's `Database.EnsureCreated()` method to scaffold the data schema and populate it with sample data.

The data itself is held in a set of CSV files in the `Autobarn.Data.Sample` folder/namespace, and is inserted using the `.HasData()` method called from the `AutobarnDbContext` class.

