namespace Jellyfin.Plugin.TagManager;

/// <summary>
/// A tag mutation request.
/// </summary>
public sealed class TagRequest
{
    /// <summary>
    /// Gets or sets the tag.
    /// </summary>
    public string? Tag { get; set; }
}
