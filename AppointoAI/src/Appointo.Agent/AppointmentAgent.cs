using Appointo.Tools;

namespace Appointo.Agent;

public sealed class AppointmentAgent
{
    private readonly IAppointmentIntentParser _parser;
    private readonly ToolGateway _toolGateway;
    private readonly Func<DateOnly> _today;

    public AppointmentAgent(IAppointmentIntentParser parser, ToolGateway toolGateway, Func<DateOnly>? today = null)
    {
        _parser = parser;
        _toolGateway = toolGateway;
        _today = today ?? (() => DateOnly.FromDateTime(DateTime.Now));
    }

    public async Task<string> HandleAsync(string message, ConversationState state, UserContext user, CancellationToken cancellationToken = default)
    {
        var parsed = await _parser.ParseAsync(message, _today(), cancellationToken);
        MergeIntoState(state, parsed);

        var current = BuildCurrentResult(state);
        state.MissingFields.Clear();
        state.MissingFields.AddRange(current.MissingFields);

        if (current.Intent == AppointmentIntent.Unknown)
        {
            state.LastQuestion = "Bu istegi randevu islemi olarak anlayamadim. Randevu almak, iptal etmek veya musait saat sormak ister misiniz?";
            return state.LastQuestion;
        }

        if (current.MissingFields.Count > 0)
        {
            state.LastQuestion = BuildClarificationQuestion(current.MissingFields);
            return state.LastQuestion;
        }

        if (current.Intent == AppointmentIntent.CreateAppointment)
        {
            var request = new CreateAppointmentToolRequest(current.CustomerName!, current.PhoneNumber!, current.ServiceType!, current.Date!.Value, current.Time!.Value, current.Notes);
            var result = await _toolGateway.ExecuteAsync(AppointmentToolNames.CreateAppointment, request, user, cancellationToken);
            if (result.Success)
            {
                ClearState(state);
                return result.Message;
            }

            return $"Randevu olusturulamadi: {result.Message}";
        }

        if (current.Intent == AppointmentIntent.ListAvailableSlots)
        {
            var service = current.ServiceType ?? "danismanlik";
            var date = current.Date ?? _today();
            var request = new FindNextAvailableSlotToolRequest(date, service, current.TimePreference ?? "any");
            var result = await _toolGateway.ExecuteAsync(AppointmentToolNames.FindNextAvailableSlot, request, user, cancellationToken);
            return result.Message;
        }

        return "Bu intent algilandi ancak ilgili akisin tam uygulamasi sonraki fazda genisletilecek.";
    }

    private static string BuildClarificationQuestion(IReadOnlyList<string> missingFields)
    {
        if (missingFields.Contains("customerName") || missingFields.Contains("phoneNumber")) return "Randevuyu olusturabilmem icin ad soyad ve telefon numaranizi alabilir miyim?";
        if (missingFields.Contains("serviceType")) return "Hangi hizmet icin randevu almak istiyorsunuz?";
        if (missingFields.Contains("date")) return "Randevu icin hangi gunu tercih ediyorsunuz?";
        return "Randevu icin hangi saati tercih ediyorsunuz?";
    }

    private static void MergeIntoState(ConversationState state, AppointmentIntentResult parsed)
    {
        if (parsed.Intent != AppointmentIntent.Unknown)
        {
            state.Intent = parsed.Intent;
        }

        StoreIfPresent(state, "customerName", parsed.CustomerName);
        StoreIfPresent(state, "phoneNumber", parsed.PhoneNumber);
        StoreIfPresent(state, "serviceType", parsed.ServiceType);
        StoreIfPresent(state, "date", parsed.Date?.ToString("yyyy-MM-dd"));
        StoreIfPresent(state, "time", parsed.Time?.ToString("HH:mm"));
        StoreIfPresent(state, "timePreference", parsed.TimePreference);
        StoreIfPresent(state, "notes", parsed.Notes);
    }

    private static AppointmentIntentResult BuildCurrentResult(ConversationState state)
    {
        var intent = state.Intent;
        var customerName = Get(state, "customerName");
        var phoneNumber = Get(state, "phoneNumber");
        var serviceType = Get(state, "serviceType");
        var date = DateOnly.TryParse(Get(state, "date"), out var parsedDate) ? parsedDate : (DateOnly?)null;
        var time = TimeOnly.TryParse(Get(state, "time"), out var parsedTime) ? parsedTime : (TimeOnly?)null;
        var timePreference = Get(state, "timePreference");
        var notes = Get(state, "notes");
        var missingFields = BuildMissingFields(intent, customerName, phoneNumber, serviceType, date, time);

        return new AppointmentIntentResult(intent, customerName, phoneNumber, serviceType, date, time, timePreference, notes, missingFields);
    }

    private static IReadOnlyList<string> BuildMissingFields(
        AppointmentIntent intent,
        string? customerName,
        string? phoneNumber,
        string? serviceType,
        DateOnly? date,
        TimeOnly? time)
    {
        if (intent is not AppointmentIntent.CreateAppointment)
        {
            return [];
        }

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(customerName)) missing.Add("customerName");
        if (string.IsNullOrWhiteSpace(phoneNumber)) missing.Add("phoneNumber");
        if (string.IsNullOrWhiteSpace(serviceType)) missing.Add("serviceType");
        if (date is null) missing.Add("date");
        if (time is null) missing.Add("time");
        return missing;
    }

    private static void StoreIfPresent(ConversationState state, string field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            state.CollectedFields[field] = value.Trim();
        }
    }

    private static string? Get(ConversationState state, string field)
    {
        return state.CollectedFields.TryGetValue(field, out var value) ? value : null;
    }

    private static void ClearState(ConversationState state)
    {
        state.Intent = AppointmentIntent.Unknown;
        state.CollectedFields.Clear();
        state.MissingFields.Clear();
        state.LastQuestion = null;
    }
}
