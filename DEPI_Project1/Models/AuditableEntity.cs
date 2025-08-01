using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using DEPI_Project1.Models;
public abstract class AuditableEntity
{
    [MaxLength(450)] // Standard length for AspNetUsers.Id
    public string? CreatedByUserId { get; set; }

    [ValidateNever]
    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? ModifiedByUserId { get; set; }

    [ValidateNever]
    public ApplicationUser? ModifiedByUser { get; set; }

    public DateTime? ModifiedAt { get; set; }
}