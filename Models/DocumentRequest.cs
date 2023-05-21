namespace Kosync.Models;

public class DocumentRequest
{
    public string document { get; set; } = default!;

    public string progress { get; set; } = default!;

    public decimal percentage { get; set; } = default!;

    public string device { get; set; } = default!;

    public string device_id { get; set; } = default!;
}