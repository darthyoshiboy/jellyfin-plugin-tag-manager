using System.Collections.Generic;

namespace Jellyfin.Plugin.TagManager;

/// <summary>
/// A page of items shown by the tag editor.
/// </summary>
public sealed record TagItemPage(IReadOnlyList<TagItem> Items, int TotalCount, bool HasMore);
