using System.Text.Encodings.Web;
using System.Text.Json;

namespace Appointo.Agent;

public static class StructuredOutputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    public static string ToJson(AppointmentIntentResult result)
    {
        var output = new
        {
            intent = ToSnakeCase(result.Intent),
            customerName = result.CustomerName,
            phoneNumber = result.PhoneNumber,
            serviceType = result.ServiceType,
            requestedDate = result.Date?.ToString("yyyy-MM-dd"),
            requestedTime = result.Time?.ToString("HH:mm"),
            timePreference = result.TimePreference,
            notes = result.Notes,
            missingFields = result.MissingFields
        };

        return JsonSerializer.Serialize(output, JsonOptions);
    }

    private static string ToSnakeCase(AppointmentIntent intent)
    {
        return intent switch
        {
            AppointmentIntent.CreateAppointment => "create_appointment",
            AppointmentIntent.CancelAppointment => "cancel_appointment",
            AppointmentIntent.RescheduleAppointment => "reschedule_appointment",
            AppointmentIntent.ListAvailableSlots => "list_available_slots",
            AppointmentIntent.GetAppointmentDetail => "get_appointment_detail",
            AppointmentIntent.GetServiceInformation => "get_service_information",
            _ => "unknown"
        };
    }
}
