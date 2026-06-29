using Appointo.Tools;

namespace Appointo.Agent;

public sealed class AppointmentAgent
{
    private readonly StructuredAppointmentParser _parser;
    private readonly ToolGateway _toolGateway;
    private readonly Func<DateOnly> _today;

    public AppointmentAgent(StructuredAppointmentParser parser, ToolGateway toolGateway, Func<DateOnly>? today = null)
    {
        _parser = parser;
        _toolGateway = toolGateway;
        _today = today ?? (() => DateOnly.FromDateTime(DateTime.Now));
    }

    public async Task<string> HandleAsync(string message, ConversationState state, UserContext user, CancellationToken cancellationToken = default)
    {
        var parsed = _parser.Parse(message, _today());
        state.Intent = parsed.Intent;
        state.MissingFields.Clear();
        state.MissingFields.AddRange(parsed.MissingFields);

        if (parsed.Intent == AppointmentIntent.Unknown)
        {
            state.LastQuestion = "Bu istegi randevu islemi olarak anlayamadim. Randevu almak, iptal etmek veya musait saat sormak ister misiniz?";
            return state.LastQuestion;
        }

        if (parsed.MissingFields.Count > 0)
        {
            state.LastQuestion = BuildClarificationQuestion(parsed.MissingFields);
            return state.LastQuestion;
        }

        if (parsed.Intent == AppointmentIntent.CreateAppointment)
        {
            var request = new CreateAppointmentToolRequest(parsed.CustomerName!, parsed.PhoneNumber!, parsed.ServiceType!, parsed.Date!.Value, parsed.Time!.Value, parsed.Notes);
            var result = await _toolGateway.ExecuteAsync(AppointmentToolNames.CreateAppointment, request, user, cancellationToken);
            return result.Success ? result.Message : $"Randevu olusturulamadi: {result.Message}";
        }

        if (parsed.Intent == AppointmentIntent.ListAvailableSlots)
        {
            var service = parsed.ServiceType ?? "danismanlik";
            var date = parsed.Date ?? _today();
            var request = new FindNextAvailableSlotToolRequest(date, service, parsed.TimePreference ?? "any");
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
}
