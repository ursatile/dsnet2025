using System.Data.Common;
using Autobarn.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Autobarn.Tests;

public static class TestDbContext {
	public static string GetSqliteDbName(this AutobarnDbContext context) {
		var builder = new DbConnectionStringBuilder {
			ConnectionString = context.Database.GetConnectionString()
		};
		return builder["Data Source"].ToString()!;
	}

	public static AutobarnDbContext Create(string? dbName = null) {
		dbName ??= Guid.NewGuid().ToString();
		var dbContext = Connect(dbName);
		dbContext.Database.EnsureCreated();
		return dbContext;
	}

	public static AutobarnDbContext Connect(string dbName) {
		var connectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";
		var sqliteConnection = new SqliteConnection(connectionString);
		sqliteConnection.Open();
		var cmd = new SqliteCommand("PRAGMA case_sensitive_like = false", sqliteConnection);
		cmd.ExecuteNonQuery();
		var options = new DbContextOptionsBuilder<AutobarnDbContext>().UseSqlite(sqliteConnection).Options;
		return new(options);
	}
}