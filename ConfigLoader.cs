using System.Reflection;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Utils;

namespace ColoredTracers
{
    public class ConfigLoader
    {
        private readonly string _configPath;
        private readonly JsonUtil _jsonUtil;

        public ConfigLoader(ModHelper modHelper, JsonUtil jsonUtil)
        {
            _jsonUtil = jsonUtil ?? throw new ArgumentNullException(nameof(jsonUtil));

            string modFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            _configPath = Path.Combine(modFolder, "config", "config.jsonc");
        }

        public T LoadConfig<T>()
        {
            if (!File.Exists(_configPath))
                throw new FileNotFoundException($"Config file not found at {_configPath}");

            string jsonContent = File.ReadAllText(_configPath);

            // JsonUtil automatically handles JSONC (comments, etc.)
            T? config = _jsonUtil.Deserialize<T>(jsonContent);

            if (config == null)
                throw new InvalidOperationException($"Failed to deserialize config file at {_configPath}");

            return config;
        }
    }
}