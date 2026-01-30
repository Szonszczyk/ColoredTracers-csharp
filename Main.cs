using ColoredTracers.CustomClasses;
using ColoredTracers.Loaders;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace ColoredTracers;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 97023)]
public class ColoredTracers(
    ISptLogger<ColoredTracers> logger,
    DatabaseService databaseService,
    ModHelper modHelper
) : IOnLoad
{
    public Task OnLoad()
    {
        ConfigLoader configLoader = new(logger, modHelper);
        var config = configLoader.Config;
        if (config.ModEnabled == false) return Task.CompletedTask;

        if (!IsPluginLoaded() && config.VerifyColorConverterAPI)
        {
            logger.LogWithColor($"[{GetType().Namespace}] Mod failed to load because ColorConverterAPI is missing from BepInEx/plugins folder. You can download it at this link: https://forge.sp-tarkov.com/mod/1090/color-converter-api", LogTextColor.Red);
            return Task.CompletedTask;
        }

        CustomBulletsManager customBulletsManager = new(logger, configLoader, databaseService);

        var tracerConfig = config.Tracers;
        var bgConfig = config.BackgroundColor;
        customBulletsManager.LoadBullets();

        string? bgModeDescription = null;
        string? bgMode = null;
        if (bgConfig.PenetrationValueModeEnabled)
        {
            bgModeDescription = "colors based on penetration value";
            bgMode = "penetration";
        }
        else if (bgConfig.BulletRatingModeEnabled)
        {
            bgModeDescription = "colors based on bullet rating";
            bgMode = "rating";
        }

        string? tracerModeDescription;
        string? tracerMode;
        string? tracerColor = null;

        if (tracerConfig.RandomModeEnabled)
        {
            tracerModeDescription = "random colors";
            tracerMode = "random";
            tracerColor = "random";
        }
        else if (tracerConfig.SameColorModeEnabled)
        {
            tracerModeDescription = $"same color {tracerConfig.SameColorValue}";
            tracerMode = "same";
            tracerColor = tracerConfig.SameColorValue;
        }
        else if (tracerConfig.BulletRatingModeEnabled)
        {
            tracerModeDescription = "colors based on bullet rating";
            tracerMode = "rating";
        }
        else
        {
            tracerModeDescription = "colors based on penetration value";
            tracerMode = "penetration";
        }

        customBulletsManager.ColorTracersAndBackgrounds(bgMode, tracerMode, tracerColor);
        if (tracerModeDescription is not null)
            logger.LogWithColor($"[{GetType().Namespace}] Added {tracerModeDescription} tracers to {customBulletsManager.tracersChanged} bullets!", LogTextColor.Green);
        if (bgModeDescription is not null)
            logger.LogWithColor($"[{GetType().Namespace}] Changed background {bgModeDescription} for {customBulletsManager.bgChanged} bullets!", LogTextColor.Green);

        return Task.CompletedTask;
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