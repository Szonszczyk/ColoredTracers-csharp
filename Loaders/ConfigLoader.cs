using ColoredTracers.Interfaces;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;
using System.Reflection;

namespace ColoredTracers.Loaders
{
    public class ConfigLoader
    {
        public ConfigData Config { get; }

        public ConfigLoader(ISptLogger<ColoredTracers> logger, ModHelper modHelper)
        {
            string modFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            string configDir = Path.Combine(modFolder, "config");
            string configPath = Path.Combine(configDir, "config.json");
            string defaultConfigPath = Path.Combine(configDir, "defaultConfig.json");

            try
            {
                // Check if config.jsonc exists
                if (!File.Exists(configPath))
                {
                    if (File.Exists(defaultConfigPath))
                    {
                        logger.LogWithColor($"[{GetType().Namespace}] Config file not found. Copying defaultConfig.json to config.jsonc...", LogTextColor.Yellow);
                        File.Copy(defaultConfigPath, configPath);
                    }
                    else
                    {
                        logger.LogWithColor($"[{GetType().Namespace}] Neither config.jsonc nor defaultConfig.json found in {configDir}. Using built-in defaults.", LogTextColor.Red);
                        Config = new ConfigData();
                        return;
                    }
                }

                // Load config.jsonc
                var config = modHelper.GetJsonDataFromFile<ConfigData>(modFolder, configPath);

                if (config == null)
                {
                    logger.LogWithColor($"[{GetType().Namespace}] Config file is null. Loading default config.", LogTextColor.Red);
                    Config = new ConfigData();
                    return;
                }

                Config = config;
                //logger.LogWithColor($"[{GetType().Namespace}] Config loaded successfully.", LogTextColor.Green);
            }
            catch (Exception ex)
            {
                logger.LogWithColor($"[{GetType().Namespace}] Failed to load config: {ex.Message}", LogTextColor.Red);
                Config = new ConfigData();
            }
        }
    }
}
