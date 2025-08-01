using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using DEPI_Project1.Utilities;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
[Index(nameof(Explanation), IsUnique = false)]
[Index(nameof(ExplanationNormalized), IsUnique = false, Name = "IX_WordExplanation_ExplanationNormalized")]
public class WordExplanation : AuditableEntity
{
    [Key]
    public int ID { get; set; }
    public string Explanation { get; set; }
    public string? ExplanationNormalized { get; set; }
    public string Language { get; set; }
    public string? Notes { get; set; } // Nullable

    // Foreign Key
    public int WordID { get; set; }
    [ValidateNever]
    public Word Word { get; set; }
         public void SetExplanation(string explanation)
    {
        Explanation = explanation;
        ExplanationNormalized = TextNormalizer.Normalize(explanation);
    }

    }

