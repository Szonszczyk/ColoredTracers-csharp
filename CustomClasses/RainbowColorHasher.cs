using System.Drawing;

// Created by ChatGPT
// Prompt: Create class in C# that will return hash color from rainbow based on given value and max value.
//         For example: given 65 as value and 65 as max value, hash returned will be violet color, given 1 as value and 99 as max, hash returned will be red color

namespace ColoredTracers.CustomClasses
{
    public static class RainbowColorHasher
    {
        /// <summary>
        /// Returns a hex color from the rainbow based on value/maxValue.
        /// Red -> Violet
        /// </summary>
        public static string GetColor(int value, int maxValue)
        {
            if (maxValue <= 0) maxValue = 1;

            if (value <= 40) value /= 2; else value -= 20;
            maxValue -= 20;

            value = Math.Clamp(value, 0, maxValue);

            // Normalize value to 0–1
            double t = (double)value / maxValue;

            // Hue from Red (0°) to Violet (270°)
            double hue = 270.0 * t;

            Color color = ColorFromHSV(hue, 1.0, 1.0);
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value *= 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            return hi switch
            {
                0 => Color.FromArgb(255, v, t, p),
                1 => Color.FromArgb(255, q, v, p),
                2 => Color.FromArgb(255, p, v, t),
                3 => Color.FromArgb(255, p, q, v),
                4 => Color.FromArgb(255, t, p, v),
                _ => Color.FromArgb(255, v, p, q),
            };
        }
    }
}