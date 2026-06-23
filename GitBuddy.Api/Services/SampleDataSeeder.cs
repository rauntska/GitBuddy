using GitBuddy.Domain.Data;
using GitBuddy.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GitBuddy.Api.Services;

public class SampleDataSeeder
{
    public static async Task SeedFileDiffsAsync(AppDbContext context)
    {
        var prs = await context.PullRequests.ToListAsync();
        
        if (prs.Count == 0) return;

        foreach (var pr in prs)
        {
            // Check if file diffs already exist
            var existingDiffs = await context.FileDiffs
                .Where(f => f.PullRequestId == pr.Id)
                .CountAsync();

            if (existingDiffs > 0) continue;

            // Create sample file diffs
            var fileDiffs = new List<FileDiff>
            {
                new FileDiff
                {
                    PullRequestId = pr.Id,
                    Path = "src/components/Button.vue",
                    Status = "modified",
                    Additions = 15,
                    Deletions = 8,
                    Changes = 23,
                    Language = "vue",
                    Patch = @"@@ -1,10 +1,15 @@
 <template>
-  <button class=""btn"">
+  <button :class=""['btn', variant]"">
     {{ label }}
   </button>
 </template>

 <script setup>
-defineProps(['label'])
+defineProps({
+  label: String,
+  variant: {
+    type: String,
+    default: 'primary'
+  }
+})
 </script>"
                },
                new FileDiff
                {
                    PullRequestId = pr.Id,
                    Path = "src/utils/helpers.ts",
                    Status = "added",
                    Additions = 25,
                    Deletions = 0,
                    Changes = 25,
                    Language = "typescript",
                    Patch = @"@@ -0,0 +1,25 @@
+export function formatDate(date: Date): string {
+  return new Intl.DateTimeFormat('en-US', {
+    year: 'numeric',
+    month: 'long',
+    day: 'numeric'
+  }).format(date);
+}
+
+export function capitalize(str: string): string {
+  return str.charAt(0).toUpperCase() + str.slice(1);
+}
+
+export function debounce<T extends (...args: any[]) => any>(
+  func: T,
+  wait: number
+): (...args: Parameters<T>) => void {
+  let timeout: NodeJS.Timeout;
+  return (...args: Parameters<T>) => {
+    clearTimeout(timeout);
+    timeout = setTimeout(() => func(...args), wait);
+  };
+}"
                },
                new FileDiff
                {
                    PullRequestId = pr.Id,
                    Path = "README.md",
                    Status = "modified",
                    Additions = 3,
                    Deletions = 1,
                    Changes = 4,
                    Language = "markdown",
                    Patch = @"@@ -12,7 +12,9 @@
 
 ## Getting Started
 
-Run `npm install` to install dependencies.
+Run the following commands to get started:
+- `npm install` - Install dependencies
+- `npm run dev` - Start development server
 
 ## License
 "
                }
            };

            context.FileDiffs.AddRange(fileDiffs);
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedCommentsAsync(AppDbContext context)
    {
        var prs = await context.PullRequests.ToListAsync();
        
        if (prs.Count == 0) return;

        foreach (var pr in prs)
        {
            // Check if comments already exist
            var existingComments = await context.Comments
                .Where(c => c.PullRequestId == pr.Id)
                .CountAsync();

            if (existingComments > 0) continue;

            // Create sample comments
            var comments = new List<Comment>
            {
                new Comment
                {
                    PullRequestId = pr.Id,
                    GitHubId = DateTime.UtcNow.Ticks,
                    Author = "Reviewer1",
                    Body = "Great work! This looks good to me.",
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    IsOutdated = false
                },
                new Comment
                {
                    PullRequestId = pr.Id,
                    GitHubId = DateTime.UtcNow.Ticks + 1,
                    Author = "Reviewer2",
                    Body = "Can we add unit tests for this?",
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    Path = "src/components/Button.vue",
                    Line = 10,
                    IsOutdated = false
                }
            };

            context.Comments.AddRange(comments);
        }

        await context.SaveChangesAsync();
    }
}
