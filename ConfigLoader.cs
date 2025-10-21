using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using SPTarkov.Server.Core.Helpers;

namespace ColoredTracers
{
    public class ConfigLoader
    {
        private readonly string _configPath;

        public ConfigLoader(ModHelper modHelper)
        {
            string modFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            _configPath = Path.Combine(modFolder, "config", "config.jsonc");
        }

        public T LoadConfig<T>()
        {
            if (!File.Exists(_configPath))
                throw new FileNotFoundException($"Config file not found at {_configPath}");

            string jsonc = File.ReadAllText(_configPath);

            // Remove // and /* */ comments before parsing
            string cleanedJson = Regex.Replace(jsonc, @"\/\/.*|\/\*[\s\S]*?\*\/", "");

            return JsonSerializer.Deserialize<T>(cleanedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            }) ?? throw new InvalidOperationException("Failed to deserialize config.");
        }
    }
}
