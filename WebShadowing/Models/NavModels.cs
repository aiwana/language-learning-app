namespace WebShadowing.Models;

public class UserNavStatsViewModel
{
    public int Streak { get; set; }
    public int Hearts { get; set; }
    public int Exp { get; set; }
    public bool IsVip { get; set; }
}

public static class NavDefaults
{
    /// <summary>Placeholder stats for nav UI preview (#9) — not from DB until auth (#10).</summary>
    public static UserNavStatsViewModel PreviewNavStats { get; } = new()
    {
        Streak = 3,
        Hearts = 5,
        Exp = 120,
        IsVip = false
    };
}
