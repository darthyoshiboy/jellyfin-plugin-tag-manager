using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.TagManager;

/// <summary>
/// Provides tag lookup and editing for library items.
/// </summary>
[ApiController]
[Route("Plugins/TagManager")]
[Authorize(Policy = Policies.RequiresElevation)]
public class TagManagerController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagManagerController"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    public TagManagerController(ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Gets every distinct tag currently used by a library item.
    /// </summary>
    /// <returns>The sorted tag names.</returns>
    [HttpGet("Tags")]
    public IReadOnlyList<string> GetTags()
    {
        return _libraryManager.GetItemList(new InternalItemsQuery { Recursive = true })
            .SelectMany(item => item.Tags ?? Array.Empty<string>())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Searches library items for the tag editor.
    /// </summary>
    /// <param name="search">Optional title search.</param>
    /// <returns>Matching items and their tags.</returns>
    [HttpGet("Items")]
    public IReadOnlyList<TagItem> GetItems([FromQuery] string? search = null)
    {
        var items = _libraryManager.GetItemList(new InternalItemsQuery { Recursive = true });
        var normalizedSearch = search?.Trim();

        return items
            .Where(item => string.IsNullOrEmpty(normalizedSearch)
                || item.Name?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) == true)
            .OrderBy(item => item.SortName ?? item.Name, StringComparer.OrdinalIgnoreCase)
            .Take(200)
            .Select(item => new TagItem(item.Id, item.Name ?? string.Empty, item.Tags ?? Array.Empty<string>()))
            .ToArray();
    }

    /// <summary>
    /// Adds a tag to an item. Existing tags are left unchanged.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="request">The tag to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content when the tag is saved.</returns>
    [HttpPost("Items/{itemId:guid}/Tags")]
    public async Task<IActionResult> AddTag(Guid itemId, [FromBody] TagRequest request, CancellationToken cancellationToken)
    {
        var tag = request.Tag?.Trim();
        if (string.IsNullOrWhiteSpace(tag))
        {
            return BadRequest("A tag is required.");
        }

        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        var tags = (item.Tags ?? Array.Empty<string>()).ToList();
        if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            tags.Add(tag);
            item.Tags = tags.ToArray();
            await _libraryManager.UpdateItemAsync(
                item,
                item.GetParent() ?? _libraryManager.RootFolder,
                ItemUpdateType.MetadataEdit,
                cancellationToken).ConfigureAwait(false);
        }

        return NoContent();
    }

    /// <summary>
    /// Removes a tag from an item.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="tag">The tag to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content when the tag is saved.</returns>
    [HttpDelete("Items/{itemId:guid}/Tags")]
    public async Task<IActionResult> RemoveTag(Guid itemId, [FromQuery] string tag, CancellationToken cancellationToken)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return NotFound();
        }

        item.Tags = (item.Tags ?? Array.Empty<string>())
            .Where(existingTag => !string.Equals(existingTag, tag?.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
        await _libraryManager.UpdateItemAsync(
            item,
            item.GetParent() ?? _libraryManager.RootFolder,
            ItemUpdateType.MetadataEdit,
            cancellationToken).ConfigureAwait(false);

        return NoContent();
    }
}
