using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SharedNotes.Api.Models;

namespace SharedNotes.Tests;

public sealed class EndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task TeamsCreateAndListEndpointsWork()
    {
        var create = await _client.PostAsJsonAsync("/api/v1/teams",
            new CreateTeamRequest { Name = $"Team {Guid.NewGuid():N}" });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var team = await create.Content.ReadFromJsonAsync<TeamResponse>();
        Assert.NotNull(team);

        var list = await _client.GetFromJsonAsync<IReadOnlyCollection<TeamResponse>>("/api/v1/teams");
        Assert.Contains(list!, item => item.Id == team!.Id);
    }

    [Fact]
    public async Task CreateNoteRequiresUserHeader()
    {
        var teamCreate = await _client.PostAsJsonAsync("/api/v1/teams",
            new CreateTeamRequest { Name = $"Team {Guid.NewGuid():N}" });
        var team = await teamCreate.Content.ReadFromJsonAsync<TeamResponse>();

        var response = await _client.PostAsJsonAsync($"/api/v1/teams/{team!.Id}/notes",
            new CreateNoteRequest { Title = "Missing header", Content = "Details" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndReadNoteEndpointsWork()
    {
        var teamCreate = await _client.PostAsJsonAsync("/api/v1/teams",
            new CreateTeamRequest { Name = $"Team {Guid.NewGuid():N}" });
        var team = await teamCreate.Content.ReadFromJsonAsync<TeamResponse>();

        using var addRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/teams/{team!.Id}/members")
        {
            Content = JsonContent.Create(new AddTeamMemberRequest { UserId = "alice" })
        };
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(addRequest)).StatusCode);

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/teams/{team.Id}/notes")
        {
            Content = JsonContent.Create(new CreateNoteRequest { Title = "Endpoint note", Content = "Details" })
        };
        createRequest.Headers.Add("X-User-Id", "alice");
        var createdResponse = await _client.SendAsync(createRequest);

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<NoteResponse>();

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/teams/{team.Id}/notes/{created!.Id}");
        getRequest.Headers.Add("X-User-Id", "alice");
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(getRequest)).StatusCode);
    }

    [Fact]
    public async Task InvalidCreatePayloadReturnsBadRequest()
    {
        var teamCreate = await _client.PostAsJsonAsync("/api/v1/teams",
            new CreateTeamRequest { Name = $"Team {Guid.NewGuid():N}" });
        var team = await teamCreate.Content.ReadFromJsonAsync<TeamResponse>();

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/teams/{team!.Id}/notes")
        {
            Content = JsonContent.Create(new CreateNoteRequest { Title = "", Content = "" })
        };
        request.Headers.Add("X-User-Id", "alice");

        Assert.Equal(HttpStatusCode.BadRequest, (await _client.SendAsync(request)).StatusCode);
    }
}
