using ColoredTracers.CustomClasses;
using ColoredTracers.Interfaces;
using ColoredTracers.Loaders;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace ColoredTracers;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader + 3)]
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

        CustomBulletsManager customBulletsManager = new(logger, databaseService);
        customBulletsManager.LoadBullets();

        var maxValue = config.BasedOn switch
        {
            BasedOnType.Penetration => config.MaxValues.Penetration,
            BasedOnType.Damage => config.MaxValues.Damage,
            _ => config.MaxValues.Penetration
        };

        var colorProfile = config.ColorProfile switch
        {
            ColorProfilesType.Custom => config.ColorProfiles.Custom,
            ColorProfilesType.Rainbow => config.ColorProfiles.Rainbow,
            _ => config.ColorProfiles.Rainbow
        };
        var colorProfileList = colorProfile.OrderBy(x => int.Parse(x.Key))
                                           .Select(x => x.Value)
                                           .ToList();
        if (!config.UseColorScale && config.UseColorProfile)
        {
            if (!ValidateColorProfile(colorProfile))
            {
                logger.LogWithColor($"[{GetType().Namespace}] Color profile {config.ColorProfile} must be consecutive integers starting from 0!", LogTextColor.Red);
                config.UseColorProfile = false;
            }
        }

        foreach (var (_, bullet) in customBulletsManager.bullets)
        {
            if (bullet is null) continue;
            if (bullet.Tracer.Contains('#') && !config.OverwriteHexColors) continue;

            if (config.RandomModeEnabled)
            {
                customBulletsManager.ColorTracer(bullet, PickColor.GetRandomHexColor());
                continue;
            }

            if (config.UseSingleColor.Enabled)
            {
                config.UseSingleColor.Types.TryGetValue(bullet.Type, out var bulletTypeConfig);
                if (bulletTypeConfig != null && bulletTypeConfig.Enabled)
                {
                    customBulletsManager.ColorTracer(bullet, bulletTypeConfig.Color);
                    continue;
                }
            }
            double? nowValue = config.BasedOn switch
            {
                BasedOnType.Penetration => bullet.tmpItem?.Properties?.PenetrationPower,
                BasedOnType.Damage => bullet.tmpItem?.Properties?.Damage,
                _ => bullet.tmpItem?.Properties?.PenetrationPower
            };
            if (!nowValue.HasValue) continue;
            
            if (config.InvertScale)
            {
                nowValue = maxValue - Math.Min((int)nowValue, maxValue);
            }

            if (config.UseColorScale)
            {
                customBulletsManager.ColorTracer(bullet, RainbowColorHasher.GetColor((int)nowValue, maxValue));
                continue;
            }
            if (config.UseColorProfile)
            {
                double percentage = (double)nowValue / maxValue;

                int colorIndex = Math.Min(
                    (int)(percentage * colorProfileList.Count),
                    colorProfileList.Count - 1
                );
                customBulletsManager.ColorTracer(bullet, colorProfileList[colorIndex]);
            }
        }

        if (customBulletsManager.TracersChanged > 0)
            logger.LogWithColor($"[{GetType().Namespace}] Added {customBulletsManager.TracersChanged} tracers to {customBulletsManager.bullets.Count} bullets!", LogTextColor.Green);
        return Task.CompletedTask;
    }
    private static bool IsPluginLoaded()
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

    private static bool ValidateColorProfile(Dictionary<string, string> colorProfile)
    {
        var expectedKeys = Enumerable.Range(0, colorProfile.Count);

        bool valid = expectedKeys.SequenceEqual(
            colorProfile.Keys
                .Select(int.Parse)
                .OrderBy(x => x)
        );

        return valid;
    }

}