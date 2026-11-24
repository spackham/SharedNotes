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

    [Fact]
    public async Task SharedNoteFlowSupportsOwnerAndGrantedUserAcrossHttpEndpoints()
    {
        var teamCreate = await _client.PostAsJsonAsync("/api/v1/teams",
            new CreateTeamRequest { Name = $"Integration Team {Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, teamCreate.StatusCode);
        var team = await teamCreate.Content.ReadFromJsonAsync<TeamResponse>();
        Assert.NotNull(team);

        foreach (var userId in new[] { "alice", "bob" })
        {
            using var memberRequest = new HttpRequestMessage(HttpMethod.Post,
                $"/api/v1/teams/{team!.Id}/members")
            {
                Content = JsonContent.Create(new AddTeamMemberRequest { UserId = userId })
            };
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(memberRequest)).StatusCode);
        }

        using var createRequest = new HttpRequestMessage(HttpMethod.Post,
            $"/api/v1/teams/{team!.Id}/notes")
        {
            Content = JsonContent.Create(new CreateNoteRequest
            {
                Title = "Integration note",
                Content = "Owner content"
            })
        };
        createRequest.Headers.Add("X-User-Id", "alice");

        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<NoteResponse>();
        Assert.NotNull(created);

        using var shareRequest = new HttpRequestMessage(HttpMethod.Post,
            $"/api/v1/teams/{team.Id}/notes/{created!.Id}/share")
        {
            Content = JsonContent.Create(new ShareNoteRequest
            {
                UserId = "bob",
                AllowUpdates = true
            })
        };
        shareRequest.Headers.Add("X-User-Id", "alice");

        var shareResponse = await _client.SendAsync(shareRequest);
        Assert.Equal(HttpStatusCode.OK, shareResponse.StatusCode);

        using var bobUpdateRequest = new HttpRequestMessage(HttpMethod.Put,
            $"/api/v1/teams/{team.Id}/notes/{created.Id}")
        {
            Content = JsonContent.Create(new UpdateNoteRequest
            {
                Title = "Bob updated",
                Content = "Bob edit"
            })
        };
        bobUpdateRequest.Headers.Add("X-User-Id", "bob");

        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(bobUpdateRequest)).StatusCode);

        using var bobDeleteRequest = new HttpRequestMessage(HttpMethod.Delete,
            $"/api/v1/teams/{team.Id}/notes/{created.Id}");
        bobDeleteRequest.Headers.Add("X-User-Id", "bob");
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.SendAsync(bobDeleteRequest)).StatusCode);

        using var bobGetRequest = new HttpRequestMessage(HttpMethod.Get,
            $"/api/v1/teams/{team.Id}/notes/{created.Id}");
        bobGetRequest.Headers.Add("X-User-Id", "bob");

        var bobGetResponse = await _client.SendAsync(bobGetRequest);
        Assert.Equal(HttpStatusCode.OK, bobGetResponse.StatusCode);
        var bobView = await bobGetResponse.Content.ReadFromJsonAsync<NoteResponse>();
        Assert.Equal("Bob updated", bobView!.Title);

        using var ownerDeleteRequest = new HttpRequestMessage(HttpMethod.Delete,
            $"/api/v1/teams/{team.Id}/notes/{created.Id}");
        ownerDeleteRequest.Headers.Add("X-User-Id", "alice");
        Assert.Equal(HttpStatusCode.NoContent,
            (await _client.SendAsync(ownerDeleteRequest)).StatusCode);

        using var missingGetRequest = new HttpRequestMessage(HttpMethod.Get,
            $"/api/v1/teams/{team.Id}/notes/{created.Id}");
        missingGetRequest.Headers.Add("X-User-Id", "alice");
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.SendAsync(missingGetRequest)).StatusCode);
    }

    [Fact]
    public async Task ReadOnlyDirectShareAllowsReadButRejectsUpdate()
    {
        var (team, owner) = await CreateTeamWithMemberAsync("alice");
        await AddMemberAsync(team.Id, "bob");
        var created = await CreateNoteAsync(team.Id, owner, "Read only note", "Details");

        using var shareRequest = await CreateJsonRequestAsync(HttpMethod.Post,
            $"/api/v1/teams/{team.Id}/notes/{created.Id}/share",
            owner, new ShareNoteRequest { UserId = "bob", AllowUpdates = false });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(shareRequest)).StatusCode);

        using var getRequest = CreateRequest(HttpMethod.Get,
            $"/api/v1/teams/{team.Id}/notes/{created.Id}", "bob");
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(getRequest)).StatusCode);

        using var updateRequest = await CreateJsonRequestAsync(HttpMethod.Put,
            $"/api/v1/teams/{team.Id}/notes/{created.Id}", "bob",
            new UpdateNoteRequest { Title = "Denied", Content = "Read only" });
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.SendAsync(updateRequest)).StatusCode);
    }

    [Fact]
    public async Task NonOwnerCannotShareOrDeleteNote()
    {
        var (team, owner) = await CreateTeamWithMemberAsync("alice");
        await AddMemberAsync(team.Id, "bob");
        var created = await CreateNoteAsync(team.Id, owner, "Owner note", "Details");

        using var shareRequest = await CreateJsonRequestAsync(HttpMethod.Post,
            $"/api/v1/teams/{team.Id}/notes/{created.Id}/share",
            "bob", new ShareNoteRequest { UserId = "eve", AllowUpdates = true });
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.SendAsync(shareRequest)).StatusCode);

        using var deleteRequest = CreateRequest(HttpMethod.Delete,
            $"/api/v1/teams/{team.Id}/notes/{created.Id}", "bob");
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.SendAsync(deleteRequest)).StatusCode);
    }

    [Fact]
    public async Task WrongTeamAndMissingNoteReturnNotFound()
    {
        var (team, owner) = await CreateTeamWithMemberAsync("alice");
        var created = await CreateNoteAsync(team.Id, owner, "Original", "Details");

        var otherTeamCreate = await _client.PostAsJsonAsync("/api/v1/teams",
            new CreateTeamRequest { Name = $"Other {Guid.NewGuid():N}" });
        var otherTeam = await otherTeamCreate.Content.ReadFromJsonAsync<TeamResponse>();

        using var wrongTeamRequest = CreateRequest(HttpMethod.Get,
            $"/api/v1/teams/{otherTeam!.Id}/notes/{created.Id}", owner);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.SendAsync(wrongTeamRequest)).StatusCode);

        using var missingRequest = CreateRequest(HttpMethod.Get,
            $"/api/v1/teams/{team.Id}/notes/{Guid.NewGuid()}", owner);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.SendAsync(missingRequest)).StatusCode);
    }

    [Fact]
    public async Task ReSharingUserIsIdempotent()
    {
        var (team, owner) = await CreateTeamWithMemberAsync("alice");
        await AddMemberAsync(team.Id, "bob");
        var created = await CreateNoteAsync(team.Id, owner, "Share twice", "Details");

        for (var index = 0; index < 2; index++)
        {
            using var shareRequest = await CreateJsonRequestAsync(HttpMethod.Post,
                $"/api/v1/teams/{team.Id}/notes/{created.Id}/share", owner,
                new ShareNoteRequest { UserId = "bob", AllowUpdates = true });
            Assert.Equal(HttpStatusCode.OK,
                (await _client.SendAsync(shareRequest)).StatusCode);
        }

        using var getRequest = CreateRequest(HttpMethod.Get,
            $"/api/v1/teams/{team.Id}/notes/{created.Id}", "bob");
        var response = await _client.SendAsync(getRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var note = await response.Content.ReadFromJsonAsync<NoteResponse>();
        Assert.Single(note!.SharedWithUserIds);
        Assert.Contains("bob", note.AllowedUpdateUserIds);
    }

    [Fact]
    public async Task BlankUpdatePayloadAndMissingHeaderReturnBadRequest()
    {
        var (team, owner) = await CreateTeamWithMemberAsync("alice");
        var created = await CreateNoteAsync(team.Id, owner, "Original", "Details");

        using var updateRequest = await CreateJsonRequestAsync(HttpMethod.Put,
            $"/api/v1/teams/{team.Id}/notes/{created.Id}", owner,
            new UpdateNoteRequest { Title = "", Content = "" });
        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.SendAsync(updateRequest)).StatusCode);

        using var missingHeaderRequest = await CreateJsonRequestAsync(HttpMethod.Put,
            $"/api/v1/teams/{team.Id}/notes/{created.Id}", null,
            new UpdateNoteRequest { Title = "No header", Content = "Details" });
        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.SendAsync(missingHeaderRequest)).StatusCode);
    }

    [Fact]
    public async Task ConcurrentHttpCreatesProduceUniqueNotes()
    {
        var (team, owner) = await CreateTeamWithMemberAsync("alice");

        var requests = Enumerable.Range(1, 20).Select(index =>
            SendCreateAsync(team.Id, owner, $"Concurrent {index}"));

        var responses = await Task.WhenAll(requests);
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));

        var notes = await Task.WhenAll(responses.Select(response =>
            response.Content.ReadFromJsonAsync<NoteResponse>()));

        Assert.Equal(20, notes.Select(note => note!.Id).Distinct().Count());
    }

    private async Task<(TeamResponse Team, string Owner)> CreateTeamWithMemberAsync(string owner)
    {
        var teamCreate = await _client.PostAsJsonAsync("/api/v1/teams",
            new CreateTeamRequest { Name = $"Team {Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, teamCreate.StatusCode);
        var team = await teamCreate.Content.ReadFromJsonAsync<TeamResponse>();
        Assert.NotNull(team);
        await AddMemberAsync(team.Id, owner);
        return (team, owner);
    }

    private async Task AddMemberAsync(string teamId, string userId)
    {
        using var request = await CreateJsonRequestAsync(HttpMethod.Post,
            $"/api/v1/teams/{teamId}/members", null,
            new AddTeamMemberRequest { UserId = userId });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(request)).StatusCode);
    }

    private async Task<NoteResponse> CreateNoteAsync(string teamId, string userId,
        string title, string content)
    {
        using var response = await SendCreateAsync(teamId, userId, title, content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var note = await response.Content.ReadFromJsonAsync<NoteResponse>();
        Assert.NotNull(note);
        return note;
    }

    private Task<HttpResponseMessage> SendCreateAsync(string teamId, string userId, string title) =>
        SendCreateAsync(teamId, userId, title, "Details");

    private async Task<HttpResponseMessage> SendCreateAsync(string teamId, string userId,
        string title, string content)
    {
        using var request = await CreateJsonRequestAsync(HttpMethod.Post,
            $"/api/v1/teams/{teamId}/notes", userId,
            new CreateNoteRequest { Title = title, Content = content });
        return await _client.SendAsync(request);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, string? userId)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrWhiteSpace(userId)) request.Headers.Add("X-User-Id", userId);
        return request;
    }

    private Task<HttpRequestMessage> CreateJsonRequestAsync<T>(HttpMethod method, string url,
        string? userId, T body)
    {
        var request = CreateRequest(method, url, userId);
        request.Content = JsonContent.Create(body);
        return Task.FromResult(request);
    }
}
