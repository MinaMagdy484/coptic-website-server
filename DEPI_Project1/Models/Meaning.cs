using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using DEPI_Project1.Utilities;
using System.Collections.Generic;


[Index(nameof(MeaningText), IsUnique = false)]
[Index(nameof(MeaningTextNormalized), IsUnique = false, Name = "IX_Meaning_MeaningTextNormalized")]
public class Meaning : AuditableEntity
{
    [Key]
    public int ID { get; set; }
    [MaxLength(600)]

    public string MeaningText { get; set; }
    public string? MeaningTextNormalized { get; set; }
    public string? Notes { get; set; } // Nullable

    public string? Language { get; set; } // Nullable

    // Relationships
    [ValidateNever]
    public ICollection<WordMeaning> WordMeanings { get; set; }
    public int? ParentMeaningID { get; set; }
    [ValidateNever]
    public Meaning ParentMeaning { get; set; }
    [ValidateNever]
    public ICollection<Meaning> ChildMeanings { get; set; }
        
    public void SetMeaningText(string meaningText)
    {
        MeaningText = meaningText;
        MeaningTextNormalized = TextNormalizer.Normalize(meaningText);
    }
}
