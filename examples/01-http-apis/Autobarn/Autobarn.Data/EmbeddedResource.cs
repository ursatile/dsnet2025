using System.Reflection;

namespace Autobarn.Data;

public class EmbeddedResource {

	public static Stream OpenStream(string resourceFileName, Assembly? assembly = null) {
		assembly ??= Assembly.GetAssembly(typeof(EmbeddedResource));
		var name = assembly!.GetManifestResourceNames()
			.FirstOrDefault(n => n.EndsWith(resourceFileName, StringComparison.OrdinalIgnoreCase));
		if (name is null) throw new("Resource not found: " + resourceFileName);
		var stream = assembly.GetManifestResourceStream(name);
		if (stream is null) throw new("Resource not found: " + resourceFileName);
		return stream;
	}

	public static byte[] ReadBytes(string resourceFileName, Assembly? assembly = null) {
		using var ms = new MemoryStream();
		OpenStream(resourceFileName, assembly).CopyTo(ms);
		return ms.ToArray();
	}

	public static string ReadAllText(string resourceFileName, Assembly? assembly = null)
		=> new StreamReader(OpenStream(resourceFileName, assembly)).ReadToEnd();

	public static string[] ReadAllLines(string resourceFileName, Assembly? assembly = null)
		=> ReadAllText(resourceFileName, assembly).ReplaceLineEndings().Split(Environment.NewLine);

}