using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using DEPI_Project1.Utilities;
using System.Collections.Generic;

[Index(nameof(Word_text), IsUnique = false)]
[Index(nameof(Word_textNormalized), IsUnique = false, Name = "IX_Word_Word_textNormalized")]
public class Word : AuditableEntity
{
    [Key]
    [DisplayName("Word ID")]
    public int WordId { get; set; }
    [Required]
    [MaxLength(500)]
    public string Word_text { get; set; }

 [MaxLength(500)]
    public string? Word_textNormalized { get; set; }


    [Required]
    public string? Language { get; set; }

    public string? Class { get; set; }

    public string? notes { get; set; }

    public string? IPA { get; set; } // Nullable
    public string? Pronunciation { get; set; } // Nullable

    public bool? ISCompleted { get; set; }
    public bool? Review1 { get; set; }
    public bool? Review2 { get; set; }

    public int? RootID { get; set; }
    [ValidateNever]
    public Word Root { get; set; }

    public int? GroupID { get; set; }
    [ValidateNever]
    public GroupWord GroupWord { get; set; }
    [ValidateNever]
    public ICollection<WordMeaning> WordMeanings { get; set; }
    [ValidateNever]
    public ICollection<DictionaryReferenceWord> DictionaryReferenceWords { get; set; }
    [ValidateNever]
    public ICollection<WordExplanation> WordExplanations { get; set; }



    public void SetWordText(string wordText)
    {
        Word_text = wordText;
        Word_textNormalized = TextNormalizer.Normalize(wordText);
    }
}
