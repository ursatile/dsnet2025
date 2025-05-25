using AngleSharp;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Autobarn.Tests;
public class WebTests {
	private static readonly IBrowsingContext browsingContext = BrowsingContext.New(Configuration.Default);

	[Fact]
	public async Task WebsiteWorks() {
		var factory = new WebApplicationFactory<Program>();
		var client = factory.CreateClient();
		var response = await client.GetAsync("/");
		response.EnsureSuccessStatusCode();
	}

	[Fact]
	public async Task Vehicles_Page_Lists_Vehicles() {
		var factory = new WebApplicationFactory<Program>();
		var client = factory.CreateClient();
		var response = await client.GetAsync("/vehicles");
		var html = await response.Content.ReadAsStringAsync();
		var dom = await browsingContext.OpenAsync(req => req.Content(html));
		var nodes = dom.QuerySelector("li.vehicle");
		nodes.ShouldNotBeNull();
		nodes.ChildElementCount.ShouldBeGreaterThan(0);
	}
}