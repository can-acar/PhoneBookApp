namespace Shared.CrossCutting.Models;

public class Pagination<T>
{
    public List<T> Data { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    /// <summary>
    ///  Creates a new instance of Pagination with the provided data, page number, page size, and total count.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="totalCount"></param>
    public Pagination(List<T> data, int pageNumber, int pageSize, int totalCount)
    {
        Data = data;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
    }
    /// <summary>
    ///  Creates a new instance of Pagination with the provided data, page number, page size, and total count.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="totalCount"></param>
    /// <returns></returns>
    public static Pagination<T> Create(List<T> data, int pageNumber, int pageSize, int totalCount)
    {
        return new Pagination<T>(data, pageNumber, pageSize, totalCount);
    }
    /// <summary>
    ///  Creates an empty Pagination instance with the specified page number and size.
    /// This is useful for returning an empty result set when no data is available.
    /// </summary>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    public static Pagination<T> Empty(int pageNumber = 1, int pageSize = 10)
    {
        return new Pagination<T>([], pageNumber, pageSize, 0);
    }
    
    

}
