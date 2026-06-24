public enum WorldTimeOfDay
{
    Day,
    Night
}

public static class WorldTimeStore
{
    public static WorldTimeOfDay CurrentTimeOfDay { get; private set; } = WorldTimeOfDay.Day;
    public static bool SoldDuringCurrentBarVisit { get; private set; }

    public static void SetDay()
    {
        CurrentTimeOfDay = WorldTimeOfDay.Day;
    }

    public static void SetNight()
    {
        CurrentTimeOfDay = WorldTimeOfDay.Night;
    }

    public static void BeginBarVisit()
    {
        SoldDuringCurrentBarVisit = false;
    }

    public static void MarkSoldInBar()
    {
        SoldDuringCurrentBarVisit = true;
    }

    public static void Clear()
    {
        CurrentTimeOfDay = WorldTimeOfDay.Day;
        SoldDuringCurrentBarVisit = false;
    }
}
