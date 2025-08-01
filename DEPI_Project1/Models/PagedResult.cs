public class PagedResult<T>
{
    public List<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public int StartItem => TotalCount == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;
    public int EndItem => Math.Min(CurrentPage * PageSize, TotalCount);
}