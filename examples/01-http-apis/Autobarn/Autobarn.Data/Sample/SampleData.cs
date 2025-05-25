namespace Autobarn.Data.Sample;

public static class SampleData {

	public static IEnumerable<object> CarModels
		=> EmbeddedResource.ReadAllLines("carmodels.csv", typeof(SampleData).Assembly)
			.Select(line => line.Split(","))
			.Where(tokens => tokens.Length == 3)
			.Select(tokens => new { Code = tokens[0], MakeCode = tokens[1], Name = tokens[2] });

	public static IEnumerable<object> Makes
		=> EmbeddedResource.ReadAllLines("makes.csv", typeof(SampleData).Assembly)
			.Select(line => line.Split(","))
			.Where(tokens => tokens.Length == 2)
			.Select(tokens => new { Code = tokens[0], Name = tokens[1] });

	public static IEnumerable<object> Vehicles
		=> EmbeddedResource.ReadAllLines("vehicles.csv", typeof(SampleData).Assembly)
			.Select(line => line.Split(","))
			.Where(tokens => tokens.Length == 4)
			.Select(tokens => new { Registration = tokens[0], ModelCode = tokens[1], Color = tokens[2], Year = Int32.Parse(tokens[3]) });
}