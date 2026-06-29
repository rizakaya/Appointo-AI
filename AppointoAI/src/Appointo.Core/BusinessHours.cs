namespace Appointo.Core;

public sealed record BusinessHours(TimeOnly OpensAt, TimeOnly LunchStartsAt, TimeOnly LunchEndsAt, TimeOnly ClosesAt)
{
    public static BusinessHours Default { get; } = new(new TimeOnly(9, 0), new TimeOnly(12, 0), new TimeOnly(13, 0), new TimeOnly(18, 0));

    public bool IsInsideWorkingHours(TimeOnly start, TimeOnly end)
    {
        if (start < OpensAt || end > ClosesAt || end <= start)
        {
            return false;
        }

        return end <= LunchStartsAt || start >= LunchEndsAt;
    }
}
