using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using DEPI_Project1.Utilities;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

[Index(nameof(ExampleText), IsUnique = false)]
[Index(nameof(ExampleTextNormalized), IsUnique = false, Name = "IX_Example_ExampleTextNormalized")]
public class Example : AuditableEntity
{
    [Key]
    public int ID { get; set; }
    public string ExampleText { get; set; }
    public string? ExampleTextNormalized { get; set; }
    public string? Reference { get; set; }
    public string? Pronunciation { get; set; } // Nullable
    public string? Notes { get; set; } // Nullable
    public string? Language { get; set; } // Nullable

    // Foreign Key
    public int? WordMeaningID { get; set; }
    [ValidateNever]
    public WordMeaning WordMeaning { get; set; }
    public int? ParentExampleID { get; set; }
    [ValidateNever]
    public Example ParentExample { get; set; }
    [ValidateNever]
    public ICollection<Example> ChildExamples { get; set; }

    public void SetExampleText(string exampleText)
    {
        ExampleText = exampleText;
        ExampleTextNormalized = TextNormalizer.Normalize(exampleText);
    }


}
