using System.Text.Json;
using System.Text.RegularExpressions;

namespace Appointo.Agent;

public sealed class OllamaAppointmentIntentParser : IAppointmentIntentParser
{
    private const string SystemPrompt = """
        You are the structured parser for Appointo AI, a Turkish appointment assistant.
        Return only one JSON object. Do not return markdown. Do not explain.

        Valid intent values:
        - create_appointment
        - cancel_appointment
        - reschedule_appointment
        - list_available_slots
        - get_appointment_detail
        - get_service_information
        - unknown

        Required fields for create_appointment:
        - customerName
        - phoneNumber
        - serviceType
        - requestedDate
        - requestedTime

        Normalize relative Turkish dates using the provided today date.
        Use yyyy-MM-dd for requestedDate.
        Use HH:mm for requestedTime.
        Use null when a value is missing.
        missingFields must contain the missing required fields for create_appointment.

        JSON shape:
        {
          "intent": "create_appointment",
          "customerName": null,
          "phoneNumber": null,
          "serviceType": null,
          "requestedDate": null,
          "requestedTime": null,
          "timePreference": null,
          "notes": null,
          "missingFields": []
        }
        """;

    private readonly IChatCompletionClient _chatClient;
    private readonly IAppointmentIntentParser _fallbackParser;

    public OllamaAppointmentIntentParser(IChatCompletionClient chatClient, IAppointmentIntentParser? fallbackParser = null)
    {
        _chatClient = chatClient;
        _fallbackParser = fallbackParser ?? new RuleBasedAppointmentIntentParser();
    }

    public async Task<AppointmentIntentResult> ParseAsync(string message, DateOnly today, CancellationToken cancellationToken = default)
    {
        try
        {
            var userMessage = $"today: {today:yyyy-MM-dd}\nmessage: {message}";
            var response = await _chatClient.ChatAsync(SystemPrompt, userMessage, cancellationToken);
            var json = ExtractJsonObject(response);
            var parsed = JsonSerializer.Deserialize<OllamaIntentDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (parsed is null)
            {
                return await _fallbackParser.ParseAsync(message, today, cancellationToken);
            }

            return ToResult(parsed);
        }
        catch
        {
            return await _fallbackParser.ParseAsync(message, today, cancellationToken);
        }
    }

    private static AppointmentIntentResult ToResult(OllamaIntentDto dto)
    {
        var intent = ParseIntent(dto.Intent);
        var date = DateOnly.TryParse(dto.RequestedDate, out var parsedDate) ? parsedDate : (DateOnly?)null;
        var time = TimeOnly.TryParse(dto.RequestedTime, out var parsedTime) ? parsedTime : (TimeOnly?)null;
        var missingFields = NormalizeMissingFields(intent, dto, date, time);

        return new AppointmentIntentResult(
            intent,
            NullIfWhiteSpace(dto.CustomerName),
            NullIfWhiteSpace(dto.PhoneNumber),
            NullIfWhiteSpace(dto.ServiceType),
            date,
            time,
            NullIfWhiteSpace(dto.TimePreference),
            NullIfWhiteSpace(dto.Notes),
            missingFields);
    }

    private static IReadOnlyList<string> NormalizeMissingFields(OllamaIntentDto dto)
    {
        return dto.MissingFields?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }

    private static IReadOnlyList<string> NormalizeMissingFields(AppointmentIntent intent, OllamaIntentDto dto, DateOnly? date, TimeOnly? time)
    {
        var missing = NormalizeMissingFields(dto).ToList();
        if (intent is not AppointmentIntent.CreateAppointment)
        {
            return missing;
        }

        AddIfMissing(missing, string.IsNullOrWhiteSpace(dto.CustomerName), "customerName");
        AddIfMissing(missing, string.IsNullOrWhiteSpace(dto.PhoneNumber), "phoneNumber");
        AddIfMissing(missing, string.IsNullOrWhiteSpace(dto.ServiceType), "serviceType");
        AddIfMissing(missing, date is null, "date");
        AddIfMissing(missing, time is null && string.IsNullOrWhiteSpace(dto.TimePreference), "time");
        return missing;
    }

    private static void AddIfMissing(List<string> missing, bool condition, string field)
    {
        if (condition && !missing.Contains(field, StringComparer.OrdinalIgnoreCase))
        {
            missing.Add(field);
        }
    }

    private static AppointmentIntent ParseIntent(string? intent)
    {
        return intent?.Trim().ToLowerInvariant() switch
        {
            "create_appointment" => AppointmentIntent.CreateAppointment,
            "cancel_appointment" => AppointmentIntent.CancelAppointment,
            "reschedule_appointment" => AppointmentIntent.RescheduleAppointment,
            "list_available_slots" => AppointmentIntent.ListAvailableSlots,
            "get_appointment_detail" => AppointmentIntent.GetAppointmentDetail,
            "get_service_information" => AppointmentIntent.GetServiceInformation,
            _ => AppointmentIntent.Unknown
        };
    }

    private static string ExtractJsonObject(string value)
    {
        var match = Regex.Match(value, "{[\\s\\S]*}");
        return match.Success ? match.Value : value;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record OllamaIntentDto(
        string? Intent,
        string? CustomerName,
        string? PhoneNumber,
        string? ServiceType,
        string? RequestedDate,
        string? RequestedTime,
        string? TimePreference,
        string? Notes,
        IReadOnlyList<string>? MissingFields);
}
