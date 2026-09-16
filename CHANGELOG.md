# Changelog

Entries here power the in-app "what's new" changelog. Format:

```
## YYYY-MM-DD

### category: Title
Body text (markdown, including optional images).
```

Category is one of `feature`, `improvement`, `fix` (case-insensitive). Each `##` date heading can contain multiple `###` entries.

## 2026-09-16

### feature: Prose diff view for markdown PRs
Markdown files can now be diffed as formatted prose instead of raw text or an all-or-nothing rendered preview — a third view mode ("Prose") alongside the existing Source and Rendered modes. Changed words are highlighted inline within each paragraph, removed content is shown struck through instead of disappearing, and a section map above the document lists every changed section so you can jump straight to it. Purely cosmetic changes — re-wrapped paragraphs, list renumbering, trailing whitespace — are hidden by default, with a one-click "show" to reveal them per section.

![Prose diff view](/changelog/prose-diff.png)

### feature: What's new changelog
GitBuddy now shows you what's changed in the app since your last visit. Look for the "What's new" link in the header.
