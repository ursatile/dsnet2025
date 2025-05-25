#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Autobarn.Data.Entities;

public class Vehicle {
	public string? Registration { get; set; }
	public string? Color { get; set; }
	public int Year { get; set; }
	public CarModel Model { get; set; }
	public string ModelCode { get; set; }
}