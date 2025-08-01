using OfficeOpenXml;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
// Remove the System.ComponentModel using statement since we don't need it
// using System.ComponentModel;

public class ExcelImporter
{
    public List<Bible> Bible { get; set; } = new List<Bible>();
    public List<Dictionary> Dictionary { get; set; } = new List<Dictionary>();
    public List<GroupWord> Group { get; set; } = new List<GroupWord>();
    public List<Word> Words { get; set; } = new List<Word>();
    public List<Meaning> Meanings { get; set; } = new List<Meaning>();
    public List<WordMeaning> WordMeanings { get; set; } = new List<WordMeaning>();
    public List<DictionaryReferenceWord> DictionaryReferenceWords { get; set; } = new List<DictionaryReferenceWord>();

    public void ImportDataFromExcel(string filePath)
    {
        // Explicitly specify OfficeOpenXml.LicenseContext
        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

        using (var package = new ExcelPackage(new FileInfo(filePath)))
        {
            foreach (var worksheet in package.Workbook.Worksheets)
            {
                string sheetName = worksheet.Name.ToLower();

                switch (sheetName)
                {
                    case "bible":
                        ImportBible(worksheet);
                        break;
                    case "group":
                        ImportGroup(worksheet);
                        break;
                    case "word":
                        ImportWords(worksheet);
                        break;
                    case "meaning":
                        ImportMeanings(worksheet);
                        break;
                    case "wordmeanings":
                        ImportWordMeanings(worksheet);
                        break;
                    case "dictionaryreference":
                        ImportDictionaryReferenceWords(worksheet);
                        break;
                    case "dictionary":
                        ImportDictionary(worksheet);
                        break;
                    default:
                        Console.WriteLine($"Unknown sheet: {sheetName}");
                        break;
                }
            }
        }
    }

    private void ImportDictionary(ExcelWorksheet worksheet)
    {
        int rowCount = worksheet.Dimension.Rows;

        for (int row = 2; row <= rowCount; row++)
        {
            var dictionary = new Dictionary
            {
                DictionaryName = worksheet.Cells[row, 2].Text,
                Abbreviation = worksheet.Cells[row, 3].Text,
                MaxNumberOfPages = int.Parse(worksheet.Cells[row, 4].Text),
                Detils = worksheet.Cells[row, 5].Text
            };

            Dictionary.Add(dictionary);
        }
    }

    private void ImportBible(ExcelWorksheet worksheet)
    {
        int rowCount = worksheet.Dimension?.Rows ?? 0;

        for (int row = 2; row <= rowCount; row++)
        {
            try
            {
                // Skip if required cells are empty
                if (string.IsNullOrWhiteSpace(worksheet.Cells[row, 1].Text) ||
                    string.IsNullOrWhiteSpace(worksheet.Cells[row, 2].Text) ||
                    string.IsNullOrWhiteSpace(worksheet.Cells[row, 3].Text))
                {
                    Console.WriteLine($"Skipping row {row} due to missing required data");
                    continue;
                }

                var bible = new Bible
                {
                    Book = int.TryParse(worksheet.Cells[row, 1].Text, out int book) ? book : 0,
                    Chapter = int.TryParse(worksheet.Cells[row, 2].Text, out int chapter) ? chapter : 0,
                    Verse = int.TryParse(worksheet.Cells[row, 3].Text, out int verse) ? verse : 0,
                    Text = worksheet.Cells[row, 4].Text,
                    Language = worksheet.Cells[row, 5].Text,
                    Edition = worksheet.Cells[row, 6].Text
                };

                Bible.Add(bible);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing row {row}: {ex.Message}");
            }
        }
    }

    private void ImportGroup(ExcelWorksheet worksheet)
    {
        int rowCount = worksheet.Dimension.Rows;

        for (int row = 2; row <= rowCount; row++)
        {
            var group = new GroupWord
            {
                Name = worksheet.Cells[row, 2].Text
                
            };

            Group.Add(group);
        }
    }

    private void ImportWords(ExcelWorksheet worksheet)
    {
        int rowCount = worksheet.Dimension.Rows;

        for (int row = 2; row <= rowCount; row++)
        {
            var word = new Word
            {
                Word_text = worksheet.Cells[row, 2].Text,
                Class = worksheet.Cells[row, 3].Text,
                GroupID = int.Parse(worksheet.Cells[row, 5].Text),
                Language = worksheet.Cells[row, 6].Text,
                notes = null,
                IPA = null,
                Pronunciation = null,
                RootID = null
            };

            Words.Add(word);
        }
    }

    private void ImportMeanings(ExcelWorksheet worksheet)
    {
        int rowCount = worksheet.Dimension.Rows;

        for (int row = 2; row <= rowCount; row++)
        {
            var meaning = new Meaning
            {
                MeaningText = worksheet.Cells[row, 2].Text,
                Notes = null,
                Language = worksheet.Cells[row, 3].Text
            };

            Meanings.Add(meaning);
        }
    }

    private void ImportWordMeanings(ExcelWorksheet worksheet)
    {
        int rowCount = worksheet.Dimension.Rows;

        for (int row = 2; row <= rowCount; row++)
        {
            var wordMeaning = new WordMeaning
            {
                WordID = int.Parse(worksheet.Cells[row, 2].Text),
                MeaningID = int.Parse(worksheet.Cells[row, 3].Text)
            };

            WordMeanings.Add(wordMeaning);
        }
    }

    private void ImportDictionaryReferenceWords(ExcelWorksheet worksheet)
    {
        int rowCount = worksheet.Dimension.Rows;

        for (int row = 2; row <= rowCount; row++)
        {
            var dictionaryReferenceWord = new DictionaryReferenceWord
            {
                WordID = int.Parse(worksheet.Cells[row, 2].Text),
                DictionaryID = int.Parse(worksheet.Cells[row, 3].Text),
                Reference = int.Parse(worksheet.Cells[row, 4].Text),
                Column = char.Parse(worksheet.Cells[row, 5].Text)
            };

            DictionaryReferenceWords.Add(dictionaryReferenceWord);
        }
    }
}
