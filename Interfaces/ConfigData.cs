namespace ColoredTracers.Interfaces
{
    public class ConfigData
    {
        public bool ModEnabled { get; set; } = true;
        public TracersConfigData Tracers { get; set; } = new TracersConfigData();
        public BackgroundColorConfigData BackgroundColor { get; set; } = new BackgroundColorConfigData();
        public LogicConfigData Logic { get; set; } = new LogicConfigData();
    }
    public class TracersConfigData
    {
        public bool PenetrationValueModeEnabled { get; set; } = true;
        public bool BulletRatingModeEnabled { get; set; } = false;
        public bool SameColorModeEnabled { get; set; } = false;
        public string SameColorValue { get; set; } = "#FFE599";
        public bool RandomModeEnabled { get; set; } = false;
    }
    public class BackgroundColorConfigData
    {
        public bool PenetrationValueModeEnabled { get; set; } = false;
        public bool BulletRatingModeEnabled { get; set; } = false;
    }
    public class LogicConfigData
    {
        public bool OverwriteHexColors { get; set; } = false;
        public Dictionary<string, Dictionary<string, double>> RatingWeights { get; set; } = [];
    }
}