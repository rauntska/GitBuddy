using System.Text;
using System.Text.RegularExpressions;
using GitBuddy.Domain.Data;
using GitBuddy.Domain.Models;
using GitBuddy.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Services;

public interface IChangelogService
{
    Task SyncFromFileAsync();
    Task<ChangelogResponseDto> GetChangelogAsync(int userId);
    Task<DateTime> MarkSeenAsync(int userId);
}

public class ChangelogService(AppDbContext context, ILogger<ChangelogService> logger) : IChangelogService
{
    private static readonly Regex DateHeadingPattern = new(@"^##\s+(\d{4}-\d{2}-\d{2})\s*$", RegexOptions.Compiled);
    private static readonly Regex EntryHeadingPattern = new(@"^###\s+([^:]+):\s*(.+)$", RegexOptions.Compiled);
    private static readonly Regex NonSlugCharsPattern = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    public async Task SyncFromFileAsync()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "CHANGELOG.md");
        if (!File.Exists(path))
        {
            logger.LogWarning("CHANGELOG.md not found at {Path}; skipping changelog sync", path);
            return;
        }

        var parsed = ParseChangelog(await File.ReadAllTextAsync(path));
        if (parsed.Count == 0)
        {
            return;
        }

        var slugs = parsed.Select(e => e.Slug).ToList();
        var existing = await context.ChangelogEntries
            .Where(e => slugs.Contains(e.Slug))
            .ToDictionaryAsync(e => e.Slug);

        foreach (var entry in parsed)
        {
            if (existing.TryGetValue(entry.Slug, out var existingEntry))
            {
                existingEntry.Category = entry.Category;
                existingEntry.Title = entry.Title;
                existingEntry.Body = entry.Body;
                existingEntry.PublishedOn = entry.PublishedOn;
            }
            else
            {
                context.ChangelogEntries.Add(entry);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<ChangelogResponseDto> GetChangelogAsync(int userId)
    {
        var lastSeenChangelogAt = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.LastSeenChangelogAt)
            .FirstOrDefaultAsync();

        var entries = await context.ChangelogEntries
            .OrderByDescending(e => e.PublishedOn)
            .ThenByDescending(e => e.Id)
            .Select(e => new ChangelogEntryDto(e.Slug, e.PublishedOn, e.Category, e.Title, e.Body, e.CreatedAt))
            .ToListAsync();

        return new ChangelogResponseDto(entries, lastSeenChangelogAt);
    }

    public async Task<DateTime> MarkSeenAsync(int userId)
    {
        var user = await context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found");

        user.LastSeenChangelogAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return user.LastSeenChangelogAt;
    }

    private static List<ChangelogEntry> ParseChangelog(string content)
    {
        var entries = new List<ChangelogEntry>();
        var lines = content.Replace("\r\n", "\n").Split('\n');

        DateOnly? currentDate = null;
        string? currentCategory = null;
        string? currentTitle = null;
        var bodyBuilder = new StringBuilder();

        void FlushEntry()
        {
            if (currentDate is null || currentTitle is null || currentCategory is null)
            {
                return;
            }

            var body = bodyBuilder.ToString().Trim();
            var slug = $"{currentDate:yyyy-MM-dd}-{Slugify(currentTitle)}";

            entries.Add(new ChangelogEntry
            {
                Slug = slug,
                PublishedOn = currentDate.Value,
                Category = NormalizeCategory(currentCategory),
                Title = currentTitle.Trim(),
                Body = body,
                CreatedAt = DateTime.UtcNow
            });
        }

        foreach (var rawLine in lines)
        {
            var dateMatch = DateHeadingPattern.Match(rawLine);
            if (dateMatch.Success)
            {
                FlushEntry();
                currentTitle = null;
                currentCategory = null;
                bodyBuilder.Clear();
                currentDate = DateOnly.Parse(dateMatch.Groups[1].Value);
                continue;
            }

            var entryMatch = EntryHeadingPattern.Match(rawLine);
            if (entryMatch.Success)
            {
                FlushEntry();
                bodyBuilder.Clear();
                currentCategory = entryMatch.Groups[1].Value.Trim();
                currentTitle = entryMatch.Groups[2].Value.Trim();
                continue;
            }

            if (currentTitle is not null)
            {
                bodyBuilder.AppendLine(rawLine);
            }
        }

        FlushEntry();
        return entries;
    }

    private static string NormalizeCategory(string category)
    {
        var trimmed = category.Trim().ToLowerInvariant();
        return trimmed switch
        {
            "feature" => "Feature",
            "improvement" => "Improvement",
            "fix" => "Fix",
            _ => "Improvement"
        };
    }

    private static string Slugify(string title)
    {
        var lower = title.Trim().ToLowerInvariant();
        var slug = NonSlugCharsPattern.Replace(lower, "-").Trim('-');
        return slug;
    }
}
