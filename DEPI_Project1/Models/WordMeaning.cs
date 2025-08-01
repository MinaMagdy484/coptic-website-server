using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
public class WordMeaning
    {
        [Key]
        public int ID { get; set; }

        // Foreign Keys
        public int WordID { get; set; }
        [ValidateNever]
        public Word Word { get; set; }

        public int MeaningID { get; set; }
        [ValidateNever]
        public Meaning Meaning { get; set; }
        // Relationships
        [ValidateNever]
        public ICollection<Example> Examples { get; set; }
        [ValidateNever]
        public ICollection<WordMeaningBible> WordMeaningBibles { get; set; }
    }