using System.Text;
using System.Globalization;

namespace DEPI_Project1.Utilities
{
    public static class TextNormalizer
    {
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Remove diacritical marks like "̀"
            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder result = new StringBuilder();

            foreach (char c in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    result.Append(c);
                }
            }

            // Convert to lowercase (including Coptic characters from Ⲁ to ⲁ)
            string withoutDiacritics = result.ToString().Normalize(NormalizationForm.FormC);
            return withoutDiacritics.ToLowerInvariant();
        }
    }
}