namespace portfolio.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public string? Path { get; set; }

    /// <summary>The exception message — shown only while developing.</summary>
    public string? Detail { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
