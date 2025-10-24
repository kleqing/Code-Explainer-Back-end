namespace CodeExplainer.BusinessObject.Response;

public class BaseResultResponse<T>
{
    public int StatusCode { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; } 
    public List<string>? Errors { get; set; }
    public T? Data { get; set; }
}