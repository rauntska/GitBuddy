namespace GitBuddy.Api.Services;

public interface ILanguageDetectionService
{
    string DetectLanguage(string path);
}

public class LanguageDetectionService : ILanguageDetectionService
{
    public string DetectLanguage(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".cs" => "csharp",
            ".vue" => "vue",
            ".ts" => "typescript",
            ".js" => "javascript",
            ".tsx" => "typescript",
            ".jsx" => "javascript",
            ".css" => "css",
            ".scss" => "scss",
            ".sass" => "sass",
            ".less" => "less",
            ".html" => "markup",
            ".xml" => "markup",
            ".json" => "json",
            ".yml" => "yaml",
            ".yaml" => "yaml",
            ".md" => "markdown",
            ".sql" => "sql",
            ".py" => "python",
            ".rb" => "ruby",
            ".java" => "java",
            ".go" => "go",
            ".rs" => "rust",
            ".cpp" => "cpp",
            ".c" => "c",
            ".h" => "c",
            ".hpp" => "cpp",
            ".sh" => "bash",
            ".ps1" => "powershell",
            ".dockerfile" => "docker",
            _ => path.EndsWith("Dockerfile") ? "docker" : "text"
        };
    }
}
