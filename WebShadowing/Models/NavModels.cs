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
    public static UserNavStatsViewModel PreviewNavStats { get; } = new()
    {
        Streak = 3,
        Hearts = 5,
        Exp = 120,
        IsVip = false
    };
}
