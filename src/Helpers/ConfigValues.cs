using BepInEx.Configuration;

namespace BbdCards.Helpers;

internal static class ConfigValues
{
    public static ConfigEntry<float> Bind(ConfigFile config, string section, string key,
        float value, float low, float high, string description)
    {
        return config.Bind(section, key, value,
            new ConfigDescription(description, new AcceptableValueRange<float>(low, high)));
    }
}
