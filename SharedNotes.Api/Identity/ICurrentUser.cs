namespace SharedNotes.Api.Identity;

/// <summary>Provides the current request actor for the temporary demo identity model.</summary>
public interface ICurrentUser
{
    bool TryGetUserId(out string userId);
}
