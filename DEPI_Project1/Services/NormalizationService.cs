using DEPI_Project1.Models;
using DEPI_Project1.Utilities;

namespace DEPI_Project1.Services
{
    public class NormalizationService : INormalizationService
    {
        public void NormalizeEntity<T>(T entity)
        {
            if (entity == null) return;

            switch (entity)
            {
                case Word word:
                    if (!string.IsNullOrEmpty(word.Word_text))
                        word.Word_textNormalized = TextNormalizer.Normalize(word.Word_text);
                    break;

                case Bible bible:
                    if (!string.IsNullOrEmpty(bible.Text))
                        bible.TextNormalized = TextNormalizer.Normalize(bible.Text);
                    break;

                case Example example:
                    if (!string.IsNullOrEmpty(example.ExampleText))
                        example.ExampleTextNormalized = TextNormalizer.Normalize(example.ExampleText);
                    break;

                case GroupWord groupWord:
                    if (!string.IsNullOrEmpty(groupWord.Name))
                        groupWord.NameNormalized = TextNormalizer.Normalize(groupWord.Name);
                    break;

                case Meaning meaning:
                    if (!string.IsNullOrEmpty(meaning.MeaningText))
                        meaning.MeaningTextNormalized = TextNormalizer.Normalize(meaning.MeaningText);
                    break;

                case WordExplanation wordExplanation:
                    if (!string.IsNullOrEmpty(wordExplanation.Explanation))
                        wordExplanation.ExplanationNormalized = TextNormalizer.Normalize(wordExplanation.Explanation);
                    break;

                case GroupExplanation groupExplanation:
                    if (!string.IsNullOrEmpty(groupExplanation.Explanation))
                        groupExplanation.ExplanationNormalized = TextNormalizer.Normalize(groupExplanation.Explanation);
                    break;
            }
        }
    }
}