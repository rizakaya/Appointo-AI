using System.Text.RegularExpressions;

namespace Appointo.Agent;

public sealed class StructuredAppointmentParser
{
    public AppointmentIntentResult Parse(string message, DateOnly today)
    {
        var normalized = Normalize(message);
        var intent = DetectIntent(normalized);
        var service = DetectService(normalized);
        var date = DetectDate(normalized, today);
        var time = DetectTime(normalized);
        var phone = DetectPhone(message);
        var customerName = DetectCustomerName(message);
        var timePreference = DetectTimePreference(normalized);
        var missing = FindMissingFields(intent, customerName, phone, service, date, time, timePreference);

        return new AppointmentIntentResult(intent, customerName, phone, service, date, time, timePreference, null, missing);
    }

    private static AppointmentIntent DetectIntent(string normalized)
    {
        if (ContainsAny(normalized, "iptal", "sil", "vazgec")) return AppointmentIntent.CancelAppointment;
        if (ContainsAny(normalized, "ertelemek", "degistir", "tasimak", "yeniden planla")) return AppointmentIntent.RescheduleAppointment;
        if (ContainsAny(normalized, "bosluk", "musait", "uygun saat", "available")) return AppointmentIntent.ListAvailableSlots;
        if (ContainsAny(normalized, "ne kadar surer", "fiyat", "ucret", "hangi islemler")) return AppointmentIntent.GetServiceInformation;
        if (ContainsAny(normalized, "randevu al", "randevu olustur", "randevusu olustur", "randevu ayarla", "kayit", "randevu almak")) return AppointmentIntent.CreateAppointment;
        return AppointmentIntent.Unknown;
    }

    private static string? DetectService(string normalized)
    {
        var services = new[] { "dis muayenesi", "dis doktoru", "sac kesim", "arac bakim", "danismanlik" };
        return services.FirstOrDefault(normalized.Contains);
    }

    private static DateOnly? DetectDate(string normalized, DateOnly today)
    {
        if (normalized.Contains("bugun")) return today;
        if (normalized.Contains("yarin")) return today.AddDays(1);
        if (normalized.Contains("cuma")) return NextDayOfWeek(today, DayOfWeek.Friday);
        if (normalized.Contains("pazartesi")) return NextDayOfWeek(today, DayOfWeek.Monday);
        return null;
    }

    private static TimeOnly? DetectTime(string normalized)
    {
        var match = Regex.Match(normalized, @"\b(?<hour>[01]?\d|2[0-3])[:.](?<minute>[0-5]\d)\b");
        if (match.Success) return new TimeOnly(int.Parse(match.Groups["hour"].Value), int.Parse(match.Groups["minute"].Value));

        match = Regex.Match(normalized, @"\bsaat\s+(?<hour>[01]?\d|2[0-3])\b");
        return match.Success ? new TimeOnly(int.Parse(match.Groups["hour"].Value), 0) : null;
    }

    private static string? DetectPhone(string message)
    {
        var match = Regex.Match(message, @"(05\d{2})\s?(\d{3})\s?(\d{2})\s?(\d{2})");
        return match.Success ? match.Value : null;
    }

    private static string? DetectCustomerName(string message)
    {
        var match = Regex.Match(message, @"(?<name>[A-ZÇĞİÖŞÜ][a-zçğıöşü]+(?:\s+[A-ZÇĞİÖŞÜ][a-zçğıöşü]+)+)");
        return match.Success ? match.Groups["name"].Value : null;
    }

    private static string? DetectTimePreference(string normalized)
    {
        if (normalized.Contains("ogleden sonra")) return "afternoon";
        if (normalized.Contains("sabah")) return "morning";
        return null;
    }

    private static IReadOnlyList<string> FindMissingFields(AppointmentIntent intent, string? customerName, string? phone, string? service, DateOnly? date, TimeOnly? time, string? timePreference)
    {
        var missing = new List<string>();
        if (intent is not AppointmentIntent.CreateAppointment) return missing;
        if (string.IsNullOrWhiteSpace(customerName)) missing.Add("customerName");
        if (string.IsNullOrWhiteSpace(phone)) missing.Add("phoneNumber");
        if (string.IsNullOrWhiteSpace(service)) missing.Add("serviceType");
        if (date is null) missing.Add("date");
        if (time is null && timePreference is null) missing.Add("time");
        return missing;
    }

    private static bool ContainsAny(string value, params string[] needles) => needles.Any(value.Contains);

    private static DateOnly NextDayOfWeek(DateOnly today, DayOfWeek dayOfWeek)
    {
        var offset = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(offset == 0 ? 7 : offset);
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant().Replace('ı', 'i').Replace('ğ', 'g').Replace('ü', 'u').Replace('ş', 's').Replace('ö', 'o').Replace('ç', 'c');
    }
}

