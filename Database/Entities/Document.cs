namespace Kosync.Database.Entities;

public class Document
{
    public string DocumentHash { get; set; } = default!;

    public string Progress { get; set; } = default!;

    public decimal Percentage { get; set; } = default!;

    public string Device { get; set; } = default!;

    public string DeviceId { get; set; } = default!;

    public DateTime Timestamp {get;set;} = default!;
}