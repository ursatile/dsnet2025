using System.Text.Json.Serialization;

namespace Autobarn.Data.Entities;

public class Make {

	public string Code { get; set; } = default!;
	public string Name { get; set; } = default!;

	[JsonIgnore]
	public virtual ICollection<CarModel> Models { get; set; } = new HashSet<CarModel>();
}