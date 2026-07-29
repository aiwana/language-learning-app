namespace WebShadowing.Models;

/// <summary>
/// Stats displayed in the navigation bar (desktop full + mobile compact).
/// Sourced from <c>User_Statistics</c> joined with <c>Users</c>.
/// </summary>
public class UserNavStatsViewModel
{
    public int Streak  { get; set; }
    public int Hearts  { get; set; }
    public int Exp     { get; set; }
    public bool IsVip  { get; set; }
}
