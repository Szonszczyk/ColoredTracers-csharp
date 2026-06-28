using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using System.Drawing;

namespace ColoredTracers.CustomClasses
{
    internal class CustomBulletsManager(
        ISptLogger<ColoredTracers> logger,
        DatabaseService databaseService
    )
    {
        private readonly Dictionary<MongoId, TemplateItem> items = databaseService.GetItems();
        private static readonly string[] bulletTypes = ["grenade", "bullet", "buckshot"];
        private static readonly string[] defaultTracerColors = ["blue", "red", "green"];

        public readonly Dictionary<MongoId, BulletsDatabase> bullets = [];
        public int TracersChanged { get; set; } = 0;
        public class BulletsDatabase
        {
            public string Type { get; set; } = string.Empty;
            public string Tracer { get; set; } = string.Empty;
            public string BgColor { get; set; } = string.Empty;
            public string Caliber { get; set; } = string.Empty;
            public Dictionary<string, double> Stats { get; set; } = [];
            public TemplateItem tmpItem { get; set; } = new TemplateItem();
        }

        public void LoadBullets()
        {
            foreach (var (id, item) in items)
            {
                var ammotype = item?.Properties?.AmmoType;
                if (item is null || ammotype is null) continue;
                if (!bulletTypes.Contains(ammotype)) continue;

                var bullet = new BulletsDatabase
                {
                    Type = ammotype,
                    Tracer = item?.Properties?.TracerColor ?? string.Empty,
                    BgColor = item?.Properties?.BackgroundColor ?? string.Empty,
                    Caliber = item?.Properties?.Caliber ?? string.Empty,
                    tmpItem = item!
                };

                // Leave flares alone lol
                if (bullet.Caliber == "Caliber26x75") continue;

                bullets.Add(id, bullet);
            }
        }
        public void ColorTracer(BulletsDatabase bullet, string? color)
        {
            if (color == null) return;
            if (bullet?.tmpItem?.Properties is null) return;
            if (!PickColor.IsValidHexColor(color) && !defaultTracerColors.Contains(color)) return;

            bullet.tmpItem.Properties.Tracer = true;
            bullet.tmpItem.Properties.TracerColor = color;
            TracersChanged++;
        }

    }
}
