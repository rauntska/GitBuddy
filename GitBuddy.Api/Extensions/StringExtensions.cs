namespace GitBuddy.Api.Extensions;

public static class StringExtensions
{
    public static string ToPascalCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        // If already starts with uppercase, return as-is
        if (char.IsUpper(str[0]))
            return str;

        return char.ToUpperInvariant(str[0]) + str.Substring(1);
    }
}