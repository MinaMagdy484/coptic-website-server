using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using DEPI_Project1.Utilities;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
[Index(nameof(Explanation), IsUnique = false)]
[Index(nameof(ExplanationNormalized), IsUnique = false, Name = "IX_GroupExplanation_ExplanationNormalized")]

public class GroupExplanation : AuditableEntity
{
    [Key]
    public int ID { get; set; }
    public string Explanation { get; set; }
    public string? ExplanationNormalized { get; set; }

    public string Language { get; set; }
    public string? Notes { get; set; } // Nullable

    // Foreign Key
    public int GroupID { get; set; }
    [ValidateNever]
    public GroupWord GroupWord { get; set; }
        

        public void SetExplanation(string explanation)
    {
        Explanation = explanation;
        ExplanationNormalized = TextNormalizer.Normalize(explanation);
    }
    }
