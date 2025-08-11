namespace Shared.CrossCutting.Models;

public class PageResponse
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static ApiResponse<PageResponse<T>> Result<T>(List<T> items,int pageNumber, int pageSize, int totalCount, int code, string message) where T : class
    {
        var pageResponse = new PageResponse<T>(items, pageNumber, pageSize, totalCount, true, message);
        return ApiResponse.Result(pageResponse.Success, pageResponse, code, message);
    }

    public static ApiResponse<PageResponse<T>> Result<T>(Pagination<T> pagination, int pageSize, int code, string message) where T : class
    {
        var pageResponse = new PageResponse<T>(pagination.Data, pagination.PageNumber, pageSize, pagination.TotalCount, true, message);
        return ApiResponse.Result(pageResponse.Success, pageResponse, code, message);
    }
}

public class PageResponse<T> : PageResponse
    where T : class
{
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public bool Success { get; set; } = true;
    public string? Message { get; set; }

    public PageResponse(IEnumerable<T> data, int pageNumber, int pageSize, long totalCount, bool success = true, string? message = null)
    {
        Data = data;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0;
        Success = success;
        Message = message;
    }

    // Implicit operator - List<T>'den PageResponse<T>'ye dönüşüm
    public static implicit operator PageResponse<T>(List<T> data)
    {
        return new PageResponse<T>(data, 1, data.Count, data.Count, true, "İşlem başarılı");
    }


    // Implicit operator - PageResponse<T>'den ApiResponse<PageResponse<T>>'ye dönüşüm
    public static implicit operator ApiResponse<PageResponse<T>>(PageResponse<T> pageResponse)
    {
        return ApiResponse.Result(
            pageResponse.Success,
            pageResponse,
            pageResponse.Success ? 200 : 404,
            pageResponse.Message ?? (pageResponse.Success ? "İşlem başarılı" : "Veri bulunamadı")
        );
    }

    // Explicit operator - ApiResponse<PageResponse<T>>'den PageResponse<T>'ye dönüşüm
    public static explicit operator PageResponse<T>(ApiResponse<PageResponse<T>> apiResponse)
    {
        if (apiResponse.Success && apiResponse.Data != null)
        {
            return apiResponse.Data;
        }

        return new PageResponse<T>(Enumerable.Empty<T>(), 0, 0, 0, false, apiResponse.Message);
    }

    // Factory methods
    public static ApiResponse<PageResponse<T>> CreateSuccess(IEnumerable<T> data, int pageNumber, int pageSize, long totalCount, string? message = null)
    {
        var collections = new PageResponse<T>(data, pageNumber, pageSize, totalCount, true, message ?? "İşlem başarılı");

        return ApiResponse.Result<PageResponse<T>>(true, collections, 200, string.Empty);
    }

    public static PageResponse<T> CreateEmpty(int pageNumber, int pageSize, string? message = null)
    {
        return new PageResponse<T>(Enumerable.Empty<T>(), pageNumber, pageSize, 0, true, message ?? "Veri bulunamadı");
    }

    public static PageResponse<T> CreateError(int pageNumber, int pageSize, string message)
    {
        return new PageResponse<T>(Enumerable.Empty<T>(), pageNumber, pageSize, 0, false, message);
    }
}