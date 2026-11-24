namespace SharedNotes.Api.Identity;

public sealed class HeaderCurrentUser : ICurrentUser
{
    public const string HeaderName = "X-User-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HeaderCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool TryGetUserId(out string userId)
    {
        userId = _httpContextAccessor.HttpContext?.Request.Headers[HeaderName]
            .ToString().Trim() ?? string.Empty;
        return userId.Length > 0;
    }
}
