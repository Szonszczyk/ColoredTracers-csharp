namespace ColoredTracers.Interfaces;

public class ConfigData
{
    public bool ModEnabled { get; set; } = true;
    public bool UseColorScale { get; set; } = true;
    public BasedOnType BasedOn { get; set; } = BasedOnType.Penetration;
    public HashSet<string> BasedOnProperties { get; set; } = ["Penetration", "Damage"];
    public ConfigMaxValues MaxValues { get; set; } = new();
    public bool UseColorProfile { get; set; } = true;
    public ColorProfilesType ColorProfile { get; set; } = ColorProfilesType.Rainbow;
    public ConfigSingleColor UseSingleColor { get; set; } = new();
    public ColorProfilesConfig ColorProfiles { get; set; } = new();
    public bool InvertScale { get; set; } = false;
    public bool RandomModeEnabled { get; set; } = false;
    public bool OverwriteHexColors { get; set; } = false;
    public bool VerifyColorConverterAPI { get; set; } = true;
}

public class ConfigMaxValues
{
    public int Penetration { get; set; } = 65;
    public int Damage { get; set; } = 100;
}

public class ConfigSingleColor
{
    public bool Enabled { get; set; } = false;
    public Dictionary<string, ConfigBulletTypes> Types { get; set; } = [];
}

public class ConfigBulletTypes
{
    public bool Enabled { get; set; } = true;
    public string Color { get; set; } = "";
}

public class ColorProfilesConfig
{
    public Dictionary<string, string> Custom { get; set; } = [];
    public Dictionary<string, string> Rainbow { get; set; } = [];
}

public enum BasedOnType
{
    Penetration,
    Damage
}

public enum ColorProfilesType
{
    Custom,
    Rainbow
}