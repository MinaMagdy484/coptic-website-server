using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

public class AddExistingMeaningToGroupViewModel
{
    public int GroupId { get; set; }
    [Required]
    public int MeaningID { get; set; }
    public List<SelectListItem> AvailableMeanings { get; set; }
}