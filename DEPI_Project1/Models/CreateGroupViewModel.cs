public class CreateGroupViewModel
{
    public int ParentGroupID { get; set; }
    public string NewGroupName { get; set; }
    public string? OriginLanguage { get; set; }
    public string? EtymologyWord { get; set; }
    public string? Etymology { get; set; }
    public string? Notes { get; set; }
    public bool? IsCompound { get; set; }
}
