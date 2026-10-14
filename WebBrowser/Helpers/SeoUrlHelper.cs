using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WebBrowser.Helpers
{
    public static partial class SeoUrlHelper
    {
        public static string ToSlug(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "noi-dung";

            var normalized = value.Trim().ToLowerInvariant()
                .Replace('đ', 'd')
                .Replace('Đ', 'd')
                .Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                    builder.Append(character);
            }

            var slug = InvalidSlugCharacters().Replace(builder.ToString().Normalize(NormalizationForm.FormC), "-");
            slug = RepeatedHyphens().Replace(slug, "-").Trim('-');
            return string.IsNullOrEmpty(slug) ? "noi-dung" : slug;
        }

        [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
        private static partial Regex InvalidSlugCharacters();

        [GeneratedRegex("-{2,}", RegexOptions.CultureInvariant)]
        private static partial Regex RepeatedHyphens();
    }
}
