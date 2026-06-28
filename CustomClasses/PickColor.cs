using System.Text.RegularExpressions;

namespace ColoredTracers.CustomClasses;


public static class PickColor
{
    public static string GetRandomHexColor()
    {
        var random = new Random();
        int colorValue = random.Next(0x1000000);
        return $"#{colorValue:X6}";
    }
    public static bool IsValidHexColor(string color)
    {
        var hexRegex = new Regex("^#([A-Fa-f0-9]{3}|[A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$");
        return hexRegex.IsMatch(color);
    }
}