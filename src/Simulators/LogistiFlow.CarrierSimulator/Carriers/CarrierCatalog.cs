namespace LogistiFlow.CarrierSimulator.Carriers;

public static class CarrierCatalog
{
    private static readonly Dictionary<string, (string Name, int TransitBusinessDays)> Carriers = new(StringComparer.Ordinal)
    {
        ["SIMX"] = ("SimExpress", 1),
        ["RDRN"] = ("Roadrunner Ground", 4),
        ["GLBL"] = ("GlobalPost International", 6),
    };

    public static bool TryGet(string code, out string name, out int transitBusinessDays)
    {
        if (Carriers.TryGetValue(code, out var carrier))
        {
            (name, transitBusinessDays) = carrier;
            return true;
        }

        name = string.Empty;
        transitBusinessDays = 0;
        return false;
    }

    public static DateOnly AddBusinessDays(DateOnly start, int days)
    {
        var date = start;
        var added = 0;
        while (added < days)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                added++;
            }
        }

        return date;
    }
}
