namespace ScanFileFunction.Models;

public class ScanResultEvent
{
    public string FileName { get; set; } = string.Empty;
    public string Container { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "clean" lub "infected"
}
