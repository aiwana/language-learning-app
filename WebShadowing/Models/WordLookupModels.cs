namespace WebShadowing.Models;

public class WordMeaningRequest
{
    public string Word { get; set; } = string.Empty;
    public string? Context { get; set; }
}

public class WordBatchRequest
{
    public List<string> Words { get; set; } = [];
}

public class WordLookupResult
{
    public string Word { get; set; } = string.Empty;
    public string Ipa { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
}

public class WordBatchResponse
{
    public Dictionary<string, WordLookupResult> Results { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
