using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TagManager;

/// <summary>
/// An item shown by the tag editor.
/// </summary>
public sealed record TagItem(Guid Id, string Name, IReadOnlyList<string> Tags);
