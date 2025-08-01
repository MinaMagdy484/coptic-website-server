using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using DEPI_Project1.Utilities;
using System.Collections.Generic;


[Index(nameof(Name), IsUnique = false)]
[Index(nameof(NameNormalized), IsUnique = false, Name = "IX_GroupWord_NameNormalized")]
public class GroupWord : AuditableEntity
{
    [Key]
    public int ID { get; set; }
    [MaxLength(600)]

    public string Name { get; set; }
    public string? NameNormalized { get; set; }
    public string? OriginLanguage { get; set; } // Nullable
    [MaxLength(200)]
    public string? GroupType { get; set; } // Nullable
    public string? EtymologyWord { get; set; } // Nullable
    public string? Etymology { get; set; } // Nullable

    public string? Notes { get; set; } // Nullable

    // Relationships
    [ValidateNever]
    public ICollection<Word> Words { get; set; }
    [ValidateNever]
    public ICollection<GroupExplanation> GroupExplanations { get; set; }
    [ValidateNever]
    public ICollection<GroupRelation> GroupParents { get; set; }

    [ValidateNever]
    public ICollection<GroupRelation> GroupChilds { get; set; }
    
    public void SetName(string name)
    {
        Name = name;
        NameNormalized = TextNormalizer.Normalize(name);
    }
}