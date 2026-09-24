using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
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
    /// <param name="startIndex">The zero-based result offset.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <returns>Matching items and paging information.</returns>
    [HttpGet("Items")]
    public TagItemPage GetItems(
        [FromQuery] string? search = null,
        [FromQuery] int startIndex = 0,
        [FromQuery] int limit = 100)
    {
        var normalizedSearch = search?.Trim();
        var query = new InternalItemsQuery
        {
            Recursive = true,
            IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Episode, BaseItemKind.Video, BaseItemKind.MusicVideo],
            NameContains = normalizedSearch,
            StartIndex = Math.Max(0, startIndex),
            Limit = Math.Clamp(limit, 1, 200),
            EnableTotalRecordCount = true
        };
        var totalCount = _libraryManager.GetCount(query);
        var items = _libraryManager.GetItemList(query);

        var result = items
            .OrderBy(item => item.SortName ?? item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new TagItem(item.Id, item.Name ?? string.Empty, item.Tags ?? Array.Empty<string>()))
            .ToArray();

        return new TagItemPage(result, totalCount, startIndex + result.Length < totalCount);
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
