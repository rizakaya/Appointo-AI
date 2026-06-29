namespace Appointo.Core;

public sealed class ServiceCatalog
{
    private readonly Dictionary<string, int> _durations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dis muayenesi"] = 30,
        ["diş muayenesi"] = 30,
        ["dis doktoru"] = 30,
        ["diş doktoru"] = 30,
        ["sac kesim"] = 45,
        ["saç kesim"] = 45,
        ["arac bakim"] = 60,
        ["araç bakım"] = 60,
        ["danismanlik"] = 60,
        ["danışmanlık"] = 60
    };

    public int GetDurationMinutes(string serviceType)
    {
        return _durations.TryGetValue(serviceType.Trim(), out var duration) ? duration : 60;
    }
}
