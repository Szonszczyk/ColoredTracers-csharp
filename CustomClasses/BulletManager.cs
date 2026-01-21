using ColoredTracers.Loaders;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using System.Text.RegularExpressions;

namespace ColoredTracers.CustomClasses
{
    internal class CustomBulletsManager(
        ISptLogger<ColoredTracers> logger,
        ConfigLoader configLoader,
        DatabaseService databaseService
    )
    {
        private readonly Dictionary<MongoId, TemplateItem> items = databaseService.GetItems();
        private readonly HandbookBase handbook = databaseService.GetHandbook();
        private static readonly string[] bulletTypes = ["grenade", "bullet", "buckshot"];
        private static readonly string[] defaultTracerColors = ["blue", "red", "green"];

        public readonly Dictionary<MongoId, BulletsDatabase> bullets = [];
        public int tracersChanged = 0;
        public int bgChanged = 0;
        public class BulletsDatabase
        {
            public string Type { get; set; } = string.Empty;
            public string Tracer { get; set; } = string.Empty;
            public string BgColor { get; set; } = string.Empty;
            public string Caliber { get; set; } = string.Empty;
            public Dictionary<string, double> Stats { get; set; } = [];
            public TemplateItem tmpItem { get; set; } = new TemplateItem();
            public double RawScore { get; set; } = 0;
            public double FinalRating { get; set; } = 0;
        }

        public void LoadBullets()
        {
            foreach (var (id, item) in items)
            {
                var ammotype = item?.Properties?.AmmoType;
                if (item is null || ammotype is null) continue;
                if (!bulletTypes.Contains(ammotype)) continue;

                HandbookItem? itemHandbook = handbook.Items.Find(t => t.Id == id);
                if (itemHandbook is null) continue;

                var bullet = new BulletsDatabase
                {
                    Type = ammotype,
                    Tracer = item?.Properties?.TracerColor ?? string.Empty,
                    BgColor = item?.Properties?.BackgroundColor ?? string.Empty,
                    Caliber = item?.Properties?.Caliber ?? string.Empty,
                    tmpItem = item!
                };
                // Don't you dare f-ing up my rating, stupid airsoft bullet!
                if (id == "6241c316234b593b5676b637") bullet.Caliber = "Airsoft";

                // Leave flares alone lol
                if (bullet.Caliber == "Caliber26x75") continue;

                configLoader.Config.Logic.RatingWeights.TryGetValue(ammotype, out Dictionary<string, double>? weights);
                if (weights == null) continue;
                foreach (var (prop, weight) in weights)
                {
                    var value = item?.Properties?.GetType()?.GetProperty(prop)?.GetValue(item?.Properties);
                    if (value is null)
                    {
                        logger.LogWithColor($"[{GetType().Namespace}] Value for '{id}' property '{prop}' is missing!", LogTextColor.Red);
                        continue;
                    }
                    bullet.Stats.Add(prop, Convert.ToDouble(value));
                }
                bullets.Add(id, bullet);
            }
            CalculateRatings();
        }

        public void CalculateRatings()
        {
            var groups = bullets.GroupBy(b => (b.Value.Type, b.Value.Caliber));

            foreach (var group in groups)
            {
                var type = group.Key.Type;
                var caliber = group.Key.Caliber;

                if (!configLoader.Config.Logic.RatingWeights.TryGetValue(type, out var weights))
                    continue;

                var maxPerStat = new Dictionary<string, double>();

                foreach (var stat in weights.Keys)
                {
                    maxPerStat[stat] = group
                        .Select(b => b.Value.Stats.TryGetValue(stat, out var v) ? v : 0)
                        .Max();
                }

                // Raw score
                foreach (var bullet in group)
                {
                    double rawScore = 0;

                    foreach (var (stat, weight) in weights)
                    {
                        if (maxPerStat[stat] <= 0)
                            continue;

                        var value = bullet.Value.Stats.TryGetValue(stat, out var v) ? v : 0;
                        var percent = value / maxPerStat[stat] * 100;

                        rawScore += percent * weight;
                    }

                    bullet.Value.RawScore = rawScore;

                }
                var bulletsInGroup = group.ToList();

                if (bulletsInGroup.Count == 1)
                {
                    bulletsInGroup[0].Value.FinalRating = 50;
                    continue;
                }
                if (bulletsInGroup.Count == 2)
                {
                    var b1 = bulletsInGroup[0];
                    var b2 = bulletsInGroup[1];

                    var r1 = b1.Value.RawScore;
                    var r2 = b2.Value.RawScore;

                    var min = Math.Min(r1, r2);
                    var max = Math.Max(r1, r2);

                    // Prevent divide-by-zero and handle negatives safely
                    var ratio = max != 0 ? min / max : 0;
                    ratio = Math.Clamp(ratio, 0, 1);

                    var offset = (1 - ratio) * 50;

                    if (r1 > r2)
                    {
                        b1.Value.FinalRating = Math.Round(50 + offset, 2);
                        b2.Value.FinalRating = Math.Round(50 - offset, 2);
                    }
                    else
                    {
                        b1.Value.FinalRating = Math.Round(50 - offset, 2);
                        b2.Value.FinalRating = Math.Round(50 + offset, 2);
                    }

                    continue;
                }
                // 3+ bullets
                var minRaw = bulletsInGroup.Min(b => b.Value.RawScore);
                var maxRaw = bulletsInGroup.Max(b => b.Value.RawScore);
                var range = maxRaw - minRaw;

                foreach (var bullet in bulletsInGroup)
                {
                    bullet.Value.FinalRating = range > 0
                        ? Math.Round((bullet.Value.RawScore - minRaw) / range * 100, 2)
                        : 0;
                }
            }
        }

        public void ColorTracersAndBackgrounds(string? bgMode, string? tracerMode, string? tracerColor)
        {
            if (tracerColor is not null)
            {
                if (!IsValidHexColor(tracerColor) && !defaultTracerColors.Contains(tracerColor.ToLower()))
                {
                    logger.LogWithColor($"[{GetType().Namespace}] Same color mode is enabled, but choosen color is incorrect (is {tracerColor}, should be: hash or blue/red/green)! Using default (\"#FFE599\")...!", LogTextColor.Red);
                    tracerColor = "#FFE599";
                }
            }
            var logic = configLoader.Config.Logic;
            foreach (var (_, bullet) in bullets)
            {
                if (tracerMode is not null && bullet?.tmpItem?.Properties?.Tracer is not null && bullet?.tmpItem?.Properties?.TracerColor is not null)
                {
                    bullet.tmpItem.Properties.Tracer = true;
                    if (!bullet.Tracer.Contains('#') || (bullet.Tracer.Contains('#') && logic.OverwriteHexColors))
                    {
                        switch (tracerMode)
                        {
                            case "random":
                                bullet.tmpItem.Properties.TracerColor = GetRandomHexColor();  
                                break;
                            case "same":
                                bullet.tmpItem.Properties.TracerColor = tracerColor;
                                break;
                            case "penetration":
                                if (bullet?.tmpItem?.Properties?.PenetrationPower is null) continue;
                                bullet.tmpItem.Properties.TracerColor = RainbowColorHasher.GetColor((int)bullet.tmpItem.Properties.PenetrationPower, 60);
                                break;
                            case "rating":
                                bullet.tmpItem.Properties.TracerColor = RainbowColorHasher.GetColor((int)bullet.FinalRating, 100);
                                break;
                        }
                        tracersChanged++;
                    }
                }
                if (bgMode is not null && bullet?.tmpItem?.Properties?.BackgroundColor is not null)
                {
                    if (!bullet.BgColor.Contains('#') || (bullet.BgColor.Contains('#') && configLoader.Config.Logic.OverwriteHexColors))
                    {
                        switch (bgMode)
                        {
                            case "penetration":
                                if (bullet?.tmpItem?.Properties?.PenetrationPower is null) continue;
                                bullet.tmpItem.Properties.BackgroundColor = RainbowColorHasher.GetColor((int)bullet.tmpItem.Properties.PenetrationPower, 60);
                                break;
                            case "rating":
                                bullet.tmpItem.Properties.BackgroundColor = RainbowColorHasher.GetColor((int)bullet.FinalRating, 100);
                                break;
                        }
                        bgChanged++;
                    }
                }
            }
        }
        private static string GetRandomHexColor()
        {
            var random = new Random();
            int colorValue = random.Next(0x1000000);
            return $"#{colorValue:X6}";
        }
        private static bool IsValidHexColor(string color)
        {
            var hexRegex = new Regex("^#([A-Fa-f0-9]{3}|[A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$");
            return hexRegex.IsMatch(color);
        }
    }
}
