namespace ScanFileFunction.Models;

public class ScanResultEvent
{
    public string FileName { get; set; }
    public string Container { get; set; }
    public string Status { get; set; } // "clean" lub "infected"
}
