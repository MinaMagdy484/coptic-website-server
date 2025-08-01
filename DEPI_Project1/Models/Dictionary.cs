using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;


    public class Dictionary: AuditableEntity
    {
        [Key]
        public int ID { get; set; }
    public string DictionaryName { get; set; }
    public string? Abbreviation { get; set; }

    public string? Detils { get; set; }

    public int? MaxNumberOfPages { get; set; }
    public string? Notes { get; set; } // Nullable

        // Relationships
        [ValidateNever]
        public ICollection<DictionaryReferenceWord> DictionaryReferenceWords { get; set; }
    }