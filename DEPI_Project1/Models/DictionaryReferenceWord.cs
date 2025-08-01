using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;


    public class DictionaryReferenceWord: AuditableEntity
    {
        [Key]
        public int ID { get; set; }

        // Foreign Keys
        public int DictionaryID { get; set; }

        [ValidateNever]
        public Dictionary Dictionary { get; set; }


        public int WordID { get; set; }
        
        
        
        [ValidateNever]
        public Word Word { get; set; }


        public int Reference { get; set; }
        public char Column { get; set; }
}

