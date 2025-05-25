using System.Text.Json.Serialization;

namespace Autobarn.Data.Entities;

public partial class CarModel {
	public string Code { get; set; } = default!;
	public Make Make { get; set; } = default!;
	public string MakeCode { get; set; } = default!;
	public string Name { get; set; } = default!;

	[JsonIgnore]
	public virtual ICollection<Vehicle> Vehicles { get; set; } = new HashSet<Vehicle>();
}