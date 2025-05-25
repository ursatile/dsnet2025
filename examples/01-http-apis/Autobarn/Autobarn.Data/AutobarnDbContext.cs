using Autobarn.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Autobarn.Data.Sample;

namespace Autobarn.Data;

public class AutobarnDbContext(
	DbContextOptions<AutobarnDbContext> options
) : DbContext(options) {

	public virtual DbSet<Make> Makes { get; set; }
	public virtual DbSet<CarModel> Models { get; set; }
	public virtual DbSet<Vehicle> Vehicles { get; set; }

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) {
		configurationBuilder.Properties<string>().UseCollation("NOCASE");
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder) {

		modelBuilder.Entity<Make>(entity => {
			entity.HasKey(e => e.Code);
			entity.Property(e => e.Code).HasMaxLength(32).IsUnicode(false);
			entity.Property(e => e.Name).HasMaxLength(32).IsUnicode(false);
			entity.HasMany(e => e.Models).WithOne(m => m.Make).HasForeignKey(m => m.MakeCode);
		});

		modelBuilder.Entity<CarModel>(entity => {
			entity.HasKey(e => e.Code);
			entity.Property(e => e.Code).HasMaxLength(32).IsUnicode(false);
			entity.Property(e => e.MakeCode).HasMaxLength(32).IsUnicode(false);
			entity.Property(e => e.Name).HasMaxLength(32).IsUnicode(false);
			entity.HasMany(e => e.Vehicles).WithOne(v => v.Model).HasForeignKey(v => v.ModelCode);
		});

		modelBuilder.Entity<Vehicle>(entity => {
			entity.HasKey(e => e.Registration);
			entity.Property(e => e.Registration).HasMaxLength(16).IsUnicode(false);
			entity.Property(e => e.Color).HasMaxLength(32).IsUnicode(false);
			entity.Property(e => e.ModelCode).HasMaxLength(32).IsUnicode(false);
		});

		modelBuilder.Entity<Make>().HasData(SampleData.Makes);
		modelBuilder.Entity<CarModel>().HasData(SampleData.CarModels);
		modelBuilder.Entity<Vehicle>().HasData(SampleData.Vehicles);
	}
}

