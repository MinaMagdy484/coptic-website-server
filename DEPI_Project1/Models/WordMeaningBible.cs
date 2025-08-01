using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
public class WordMeaningBible: AuditableEntity
    {
        [Key]
        public int ID { get; set; }
        
        int? wordnumber { get; set; }
    // Foreign Keys
    public int WordMeaningID { get; set; }
        [ValidateNever]
        public WordMeaning WordMeaning { get; set; }

        public int BibleID { get; set; }

        [ValidateNever]
        public Bible Bible { get; set; }
    }

