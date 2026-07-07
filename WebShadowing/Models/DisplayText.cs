using System.Text;

namespace WebShadowing.Models;

public static class DisplayText
{
    public static string Fix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = value.Trim();

        if (LooksLikeUtf8Mojibake(text))
        {
            try
            {
                text = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(text));
            }
            catch
            {
                text = value.Trim();
            }
        }

        return RepairCommonVietnameseText(text);
    }

    private static string RepairCommonVietnameseText(string value)
    {
        return value
            .Replace("Ti?ng Anh l?p 6", "Ti\u1ebfng Anh l\u1edbp 6", StringComparison.OrdinalIgnoreCase)
            .Replace("Ti?ng Anh l?p", "Ti\u1ebfng Anh l\u1edbp", StringComparison.OrdinalIgnoreCase)
            .Replace("Ti?ng Anh", "Ti\u1ebfng Anh", StringComparison.OrdinalIgnoreCase)
            .Replace("l?p", "l\u1edbp", StringComparison.OrdinalIgnoreCase)
            .Replace("Gi\uFFFD o", "Gi\u00e1o", StringComparison.OrdinalIgnoreCase)
            .Replace("Gi\uFFFDo", "Gi\u00e1o", StringComparison.OrdinalIgnoreCase)
            .Replace("Gi?o", "Gi\u00e1o", StringComparison.OrdinalIgnoreCase)
            .Replace("tr\uFFFDnh", "tr\u00ecnh", StringComparison.OrdinalIgnoreCase)
            .Replace("tr?nh", "tr\u00ecnh", StringComparison.OrdinalIgnoreCase)
            .Replace("co b?n", "c\u01a1 b\u1ea3n", StringComparison.OrdinalIgnoreCase)
            .Replace("c? b?n", "c\u01a1 b\u1ea3n", StringComparison.OrdinalIgnoreCase)
            .Replace("\u00D0?i s?ng", "\u0110\u1eddi s\u1ed1ng", StringComparison.OrdinalIgnoreCase)
            .Replace("D?i s?ng", "\u0110\u1eddi s\u1ed1ng", StringComparison.OrdinalIgnoreCase)
            .Replace("H?c Thu?t", "H\u1ecdc thu\u1eadt", StringComparison.OrdinalIgnoreCase)
            .Replace("Giao ti?p", "Giao ti\u1ebfp", StringComparison.OrdinalIgnoreCase)
            .Replace("C\u00f4ng S?", "C\u00f4ng vi\u1ec7c", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeUtf8Mojibake(string value)
    {
        return value.Contains('\u00C3')
            || value.Contains('\u00C1')
            || value.Contains('\u00C2')
            || value.Contains('\u00C6')
            || value.Contains('\u00C4')
            || value.Contains('\u00D0')
            || value.Contains('\u00BA')
            || value.Contains('\u00BB');
    }
}
