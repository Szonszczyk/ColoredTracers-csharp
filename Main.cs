using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using System.Text.RegularExpressions;

namespace ColoredTracers;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 73133)]
public class ColoredTracers(ISptLogger<ColoredTracers> logger, DatabaseService databaseService, ModHelper modHelper, JsonUtil jsonUtil) : IOnLoad
{
    private readonly ConfigLoader _configLoader = new(modHelper, jsonUtil);
    private readonly string ModName = "ColoredTracers";
    private static readonly string[] defaultTracerColors = ["green", "red", "blue"];
    private static readonly string[] bulletTypes = ["grenade", "bullet", "buckshot"];

    public Task OnLoad()
    {
        var config = _configLoader.LoadConfig<ConfigData>();

        if (config.ModEnabled == false) return Task.CompletedTask;

        var modSelected = "";

        if (config.RandomMode) modSelected = "random";
        else if (config.SameColor) modSelected = "sameColor";
        else if (config.SarynMode) modSelected = "sarynMode";

        if (modSelected == "sameColor" && !IsValidHexColor(config.SameColorValue))
        {
            logger.LogWithColor($"[{ModName}] \"sameColor\" mode is enabled, but choosen color is not a valid hex value! Mod is disabled!", LogTextColor.Red);
            return Task.CompletedTask;
        }
        
        if (modSelected == "sarynMode" && !defaultTracerColors.Contains(config.SarynModeColor.ToLower()))
        {
            logger.LogWithColor($"[{ModName}] \"sarynMode\" mode is enabled, but choosen color is not green / red / blue! Mod is disabled!", LogTextColor.Red);
            return Task.CompletedTask;
        }

        if (!IsPluginLoaded() && (modSelected != "sarynMode"))
        {
            logger.LogWithColor($"[{ModName}] Mod failed to load because ColorConverterAPI is missing in BepInEx/plugins folder. You can download it at this link: https://forge.sp-tarkov.com/mod/1090/color-converter-api", LogTextColor.Red);
            return Task.CompletedTask;
        }

        Dictionary<string, string>? colorProfile = config.ColorProfile switch
        {
            "Custom" => config.ColorProfiles.Custom,
            "Rainbow" => config.ColorProfiles.Rainbow,
            "Grayscale" => config.ColorProfiles.Grayscale,
            "GrayscaleInverted" => config.ColorProfiles.GrayscaleInverted,
            _ => null
        };

        Dictionary<MongoId, TemplateItem> items = databaseService.GetItems();

        int bulletCount = 0;

        foreach (TemplateItem item in items.Values)
        {
            if (!bulletTypes.Contains(item?.Properties?.AmmoType)) continue;
            if (item?.Properties?.PenetrationPower is null) continue;

            int bulletPenetration = (int)item.Properties.PenetrationPower;
            int bulletArmorTier = CalculateVanillaArmorClass(bulletPenetration);

            string tracerColor = "";

            tracerColor = modSelected switch
            {
                "random" => GetRandomHexColor(),
                "sameColor" => config.SameColorValue,
                "sarynMode" => config.SarynModeColor.ToLower(),
                _ => colorProfile?.TryGetValue(bulletArmorTier.ToString(), out var value) == true ? value : "#FFFFFF"
            };

            if (item?.Properties?.BackgroundColor is not null && config.UseBackgroundColors && item.Properties.BackgroundColor.Contains('#'))
            {
                tracerColor = item.Properties.BackgroundColor;
            }
            if (item?.Properties?.Tracer is not null && item?.Properties?.TracerColor is not null) {
                item.Properties.Tracer = true;
                if (item.Properties.TracerColor != tracerColor)
                {
                    item.Properties.TracerColor = tracerColor;
                    bulletCount++;
                }
            }
        }
        switch (modSelected)
        {
            case "random":
                logger.LogWithColor($"[{ModName}] Added random colored tracers to {bulletCount} bullets!", LogTextColor.Green);
                break;
            case "sameColor":
                logger.LogWithColor($"[{ModName}] Added '{config.SameColorValue}' color tracers to {bulletCount} bullets!", LogTextColor.Green);
                break;
            case "sarynMode":
                logger.LogWithColor($"[{ModName}] Added '{config.SarynModeColor.ToLower()}' color tracers to {bulletCount} bullets!", LogTextColor.Green);
                break;
            default:
                logger.LogWithColor($"[{ModName}] Added penetration based color tracers to {bulletCount} bullets!", LogTextColor.Green);
                break;
        }
        return Task.CompletedTask;
    }
    public class ConfigData
    {
        public bool ModEnabled { get; set; } = true;

        public bool UseBackgroundColors { get; set; } = true;

        public bool RandomMode { get; set; } = false;

        public bool SameColor { get; set; } = false;
        public string SameColorValue { get; set; } = "#A10000";

        public bool SarynMode { get; set; } = false;
        public string SarynModeColor { get; set; } = "green";

        public string ColorProfile { get; set; } = "Rainbow";

        public ColorProfilesConfig ColorProfiles { get; set; } = new();
    }

    public class ColorProfilesConfig
    {
        public Dictionary<string, string> Custom { get; set; } = [];
        public Dictionary<string, string> Rainbow { get; set; } = [];
        public Dictionary<string, string> Grayscale { get; set; } = [];
        public Dictionary<string, string> GrayscaleInverted { get; set; } = [];
    }

    public int CalculateVanillaArmorClass(double penetrationValue)
    {
        int penTier = 1;

        while (penTier <= 6)
        {
            double armorStrength = penTier * 10;

            if (armorStrength >= penetrationValue + 15)
            {
                break;
            }

            if (armorStrength <= penetrationValue - 15)
            {
                penTier++;
                continue;
            }

            double penetrationChance;

            if (armorStrength >= penetrationValue)
            {
                penetrationChance = 0.4 * Math.Pow(armorStrength - penetrationValue - 15.0, 2);
            }
            else
            {
                penetrationChance = 100.0 + penetrationValue / (0.9 * armorStrength - penetrationValue);
            }

            if (penetrationChance >= 50.0)
            {
                penTier++;
                continue;
            }

            break;
        }

        return penTier - 1;
    }

    public string GetRandomHexColor()
    {
        var random = new Random();
        int colorValue = random.Next(0x1000000);
        return $"#{colorValue:X6}";
    }

    public bool IsValidHexColor(string color)
    {
        var hexRegex = new Regex("^#([A-Fa-f0-9]{3}|[A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$");
        return hexRegex.IsMatch(color);
    }

    public static bool IsPluginLoaded()
    {
        const string pluginName = "rairai.colorconverterapi.dll";
        const string pluginsPath = "../BepInEx/plugins";

        try
        {
            if (!Directory.Exists(pluginsPath))
                return false;

            var pluginList = Directory.GetFiles(pluginsPath)
                .Select(System.IO.Path.GetFileName)
                .Select(f => f.ToLowerInvariant());
            return pluginList.Contains(pluginName);
        }
        catch
        {
            return false;
        }
    }
}