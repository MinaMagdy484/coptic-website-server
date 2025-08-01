using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;


    public class GroupRelation: AuditableEntity
{
    [Key]
    public int ID { get; set; }

    // Self-Referencing Foreign Key
    public int ParentGroupID { get; set; }
    [ValidateNever]
    public GroupWord ParentGroup { get; set; }

    public int RelatedGroupID { get; set; }
    [ValidateNever]

    public GroupWord RelatedGroup { get; set; }
    public bool? IsCompound { get; set; }
}

