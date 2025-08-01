using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using DEPI_Project1.Utilities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;


[Index(nameof(Book), nameof(Chapter), nameof(Verse), nameof(Language), nameof(Edition))]
[Index(nameof(Text), IsUnique = false)]
[
    Index(nameof(TextNormalized), IsUnique = false, Name = "IX_Bible_TextNormalized")
]

public class Bible : AuditableEntity
{
    [Key]
    public int BibleID { get; set; }
    public int Book { get; set; }
    public int Chapter { get; set; }
    public int Verse { get; set; }
    public string Language { get; set; }
    public string Edition { get; set; }
            [Column(TypeName = "nvarchar(max)")]

    public string Text { get; set; }
            [Column(TypeName = "nvarchar(1000)")]

    public string? TextNormalized { get; set; }

    public string? Pronunciation { get; set; } // Nullable
    public string? Notes { get; set; } // Nullable

    // Relationships
    [ValidateNever]
    public ICollection<WordMeaningBible> WordMeaningBibles { get; set; }
        
    public void SetText(string text)
    {
        Text = text;
        TextNormalized = TextNormalizer.Normalize(text);
    }
    }

//public class BibleVerse
//{
//    public int BibleVerseID { get; set; }
//    public string Book { get; set; }
//    public int Chapter { get; set; }
//    public int Verse { get; set; }
//    public string Edition { get; set; }
//    public string Language { get; set; }
//    public string Text { get; set; }
//    public string Pronunciation { get; set; }

//    public ICollection<WordMeaningBible> WordMeaningBibles { get; set; }
//}