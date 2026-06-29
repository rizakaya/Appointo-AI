namespace Appointo.Agent;

public sealed class HandoffRouter
{
    private readonly IAppointmentIntentParser _parser;
    private readonly Func<DateOnly> _today;

    public HandoffRouter(IAppointmentIntentParser parser, Func<DateOnly>? today = null)
    {
        _parser = parser;
        _today = today ?? (() => DateOnly.FromDateTime(DateTime.Now));
    }

    public async Task<HandoffDecision> DecideAsync(string message, CancellationToken cancellationToken = default)
    {
        var parsed = await _parser.ParseAsync(message, _today(), cancellationToken);
        return Decide(parsed);
    }

    public HandoffDecision Decide(AppointmentIntentResult parsed)
    {
        return parsed.Intent switch
        {
            AppointmentIntent.CreateAppointment => DecideCreateAppointment(parsed),
            AppointmentIntent.ListAvailableSlots => HandoffDecision.Availability("Musaitlik ve slot hesaplama AvailabilityAgent tarafindan yonetilir."),
            AppointmentIntent.CancelAppointment => DecideCustomerSensitiveOperation(parsed, "Iptal islemi icin once musteri veya randevu dogrulamasi gerekir."),
            AppointmentIntent.RescheduleAppointment => DecideCustomerSensitiveOperation(parsed, "Yeniden planlama icin once musteri veya randevu dogrulamasi gerekir."),
            AppointmentIntent.GetAppointmentDetail => DecideCustomerSensitiveOperation(parsed, "Randevu detayi kisisel veri icerebilir; once musteri dogrulamasi gerekir."),
            AppointmentIntent.GetServiceInformation => HandoffDecision.Support("Hizmet bilgisi sorulari RAG fazina kadar SupportAgent tarafindan karsilanir."),
            _ => HandoffDecision.Support("Intent guvenle siniflandirilamadi; manuel destek veya netlestirme gerekir.")
        };
    }

    private static HandoffDecision DecideCreateAppointment(AppointmentIntentResult parsed)
    {
        if (IsMissing(parsed, "customerName") || IsMissing(parsed, "phoneNumber"))
        {
            return HandoffDecision.Customer("Randevu olusturmadan once musteri adi ve telefon bilgisi tamamlanmalidir.");
        }

        if (IsMissing(parsed, "date") || IsMissing(parsed, "time") || !string.IsNullOrWhiteSpace(parsed.TimePreference))
        {
            return HandoffDecision.Availability("Randevu tarihi, saati veya tercih edilen zaman araligi musaitlik kontrolu gerektirir.");
        }

        return HandoffDecision.Appointment("Randevu olusturmak icin gerekli bilgiler tamam; AppointmentAgent islemi yurutebilir.");
    }

    private static HandoffDecision DecideCustomerSensitiveOperation(AppointmentIntentResult parsed, string reason)
    {
        if (string.IsNullOrWhiteSpace(parsed.CustomerName) && string.IsNullOrWhiteSpace(parsed.PhoneNumber))
        {
            return HandoffDecision.Customer(reason);
        }

        return HandoffDecision.Appointment("Musteri bilgisi mevcut; AppointmentAgent randevu islemini yurutebilir.");
    }

    private static bool IsMissing(AppointmentIntentResult parsed, string field)
    {
        return parsed.MissingFields.Contains(field, StringComparer.OrdinalIgnoreCase);
    }
}
