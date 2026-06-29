using Appointo.Agent;
using Appointo.Core;
using Appointo.Tools;

var tests = new List<(string Name, Func<Task> Run)>
{
    ("Intent parser detects create appointment", IntentParserDetectsCreateAppointment),
    ("Intent parser supports Turkish characters", IntentParserSupportsTurkishCharacters),
    ("Appointment service rejects lunch break", AppointmentServiceRejectsLunchBreak),
    ("Appointment service rejects overlapping slot", AppointmentServiceRejectsOverlappingSlot),
    ("Permission matrix blocks guest cancellation", PermissionMatrixBlocksGuestCancellation),
    ("Agent asks missing details", AgentAsksMissingDetails),
    ("Agent completes appointment across turns", AgentCompletesAppointmentAcrossTurns),
    ("Structured output formats parse result", StructuredOutputFormatsParseResult),
    ("Ollama parser maps JSON response", OllamaParserMapsJsonResponse),
    ("Ollama parser falls back on invalid JSON", OllamaParserFallsBackOnInvalidJson)
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

if (failed > 0) Environment.ExitCode = 1;

static Task IntentParserDetectsCreateAppointment()
{
    var parser = new StructuredAppointmentParser();
    var result = parser.Parse("Yarin saat 14:00 icin Ahmet Kaya adina sac kesim randevusu olustur. 0555 111 22 33", new DateOnly(2026, 6, 29));
    Assert(result.Intent == AppointmentIntent.CreateAppointment, "Intent create olmali.");
    Assert(result.CustomerName == "Ahmet Kaya", "Musteri adi bulunmali.");
    Assert(result.Date == new DateOnly(2026, 6, 30), "Yarin tarihi cozulmeli.");
    Assert(result.Time == new TimeOnly(14, 0), "Saat cozulmeli.");
    return Task.CompletedTask;
}

static Task IntentParserSupportsTurkishCharacters()
{
    var parser = new StructuredAppointmentParser();
    var result = parser.Parse("Yarın saat 14:00 için Çağla Şahin adına saç kesim randevusu oluştur. 0555 333 44 55", new DateOnly(2026, 6, 29));
    var json = StructuredOutputFormatter.ToJson(result);

    Assert(result.Intent == AppointmentIntent.CreateAppointment, "Turkce karakterli intent create olmali.");
    Assert(result.CustomerName == "Çağla Şahin", "Turkce karakterli musteri adi bulunmali.");
    Assert(result.ServiceType == "sac kesim", "Turkce karakterli hizmet normalize edilmeli.");
    Assert(json.Contains("Çağla Şahin"), "JSON Turkce karakterleri okunur yazmali.");
    return Task.CompletedTask;
}

static async Task AppointmentServiceRejectsLunchBreak()
{
    var service = NewService();
    var result = await service.CreateAsync(new CreateAppointmentRequest("Ahmet Kaya", "0555 111 22 33", "dis muayenesi", new DateOnly(2026, 7, 1), new TimeOnly(12, 0), null));
    Assert(!result.Success, "Ogle arasina randevu verilmemeli.");
}

static async Task AppointmentServiceRejectsOverlappingSlot()
{
    var service = NewService();
    var first = await service.CreateAsync(new CreateAppointmentRequest("Ahmet Kaya", "0555 111 22 33", "dis muayenesi", new DateOnly(2026, 7, 1), new TimeOnly(14, 0), null));
    var second = await service.CreateAsync(new CreateAppointmentRequest("Ayse Yilmaz", "0555 222 33 44", "dis muayenesi", new DateOnly(2026, 7, 1), new TimeOnly(14, 15), null));
    Assert(first.Success, "Ilk randevu olusmali.");
    Assert(!second.Success, "Cakisan randevu reddedilmeli.");
}

static async Task PermissionMatrixBlocksGuestCancellation()
{
    var gateway = new ToolGateway(NewService());
    var result = await gateway.ExecuteAsync(AppointmentToolNames.CancelAppointment, new CancelAppointmentToolRequest(Guid.NewGuid()), UserContext.Guest);
    Assert(!result.Success, "Guest iptal tool'unu calistiramamali.");
}

static async Task AgentAsksMissingDetails()
{
    var agent = new AppointmentAgent(new RuleBasedAppointmentIntentParser(), new ToolGateway(NewService()), () => new DateOnly(2026, 6, 29));
    var response = await agent.HandleAsync("Yarin randevu almak istiyorum.", new ConversationState(), UserContext.Guest);
    Assert(response.Contains("ad soyad") || response.Contains("Hangi hizmet"), "Agent eksik bilgi sormali.");
}

static async Task AgentCompletesAppointmentAcrossTurns()
{
    var state = new ConversationState();
    var agent = new AppointmentAgent(new RuleBasedAppointmentIntentParser(), new ToolGateway(NewService()), () => new DateOnly(2026, 6, 29));

    var first = await agent.HandleAsync("Yarin randevu almak istiyorum.", state, UserContext.Guest);
    var second = await agent.HandleAsync("Ahmet Kaya 0555 111 22 33", state, UserContext.Guest);
    var third = await agent.HandleAsync("sac kesim", state, UserContext.Guest);
    var fourth = await agent.HandleAsync("saat 14:00", state, UserContext.Guest);

    Assert(first.Contains("ad soyad"), "Ilk cevap ad soyad istemeli.");
    Assert(second.Contains("Hangi hizmet"), "Ikinci cevap hizmet istemeli.");
    Assert(third.Contains("hangi saati", StringComparison.OrdinalIgnoreCase), "Ucuncu cevap saat istemeli.");
    Assert(fourth.Contains("Randevu olusturuldu"), "Son cevap randevuyu olusturmali.");
    Assert(state.Intent == AppointmentIntent.Unknown, "Basarili randevudan sonra state temizlenmeli.");
}

static Task StructuredOutputFormatsParseResult()
{
    var parser = new StructuredAppointmentParser();
    var result = parser.Parse("Yarin saat 14:00 icin Ahmet Kaya adina sac kesim randevusu olustur.", new DateOnly(2026, 6, 29));
    var json = StructuredOutputFormatter.ToJson(result);

    Assert(json.Contains("\"intent\": \"create_appointment\""), "Intent snake_case JSON olmali.");
    Assert(json.Contains("\"customerName\": \"Ahmet Kaya\""), "Musteri adi JSON'a yazilmali.");
    Assert(json.Contains("\"phoneNumber\""), "Telefon alani structured output'ta bulunmali.");
    Assert(json.Contains("phoneNumber"), "Eksik telefon bilgisi gorunmeli.");
    return Task.CompletedTask;
}

static async Task OllamaParserMapsJsonResponse()
{
    var chat = new FakeChatCompletionClient("""
        {
          "intent": "create_appointment",
          "customerName": "Ahmet Kaya",
          "phoneNumber": "0555 111 22 33",
          "serviceType": "dis muayenesi",
          "requestedDate": "2026-06-30",
          "requestedTime": "14:00",
          "timePreference": null,
          "notes": null,
          "missingFields": []
        }
        """);
    var parser = new OllamaAppointmentIntentParser(chat);

    var result = await parser.ParseAsync("Yarin randevu al.", new DateOnly(2026, 6, 29));

    Assert(result.Intent == AppointmentIntent.CreateAppointment, "Ollama intent create olmali.");
    Assert(result.CustomerName == "Ahmet Kaya", "Ollama musteri adini map etmeli.");
    Assert(result.Date == new DateOnly(2026, 6, 30), "Ollama tarihi map etmeli.");
    Assert(result.Time == new TimeOnly(14, 0), "Ollama saati map etmeli.");
}

static async Task OllamaParserFallsBackOnInvalidJson()
{
    var chat = new FakeChatCompletionClient("json degil");
    var parser = new OllamaAppointmentIntentParser(chat);

    var result = await parser.ParseAsync("Yarin saat 14:00 icin Ahmet Kaya adina sac kesim randevusu olustur. 0555 111 22 33", new DateOnly(2026, 6, 29));

    Assert(result.Intent == AppointmentIntent.CreateAppointment, "Fallback rule-based parser calismali.");
    Assert(result.CustomerName == "Ahmet Kaya", "Fallback musteri adini bulmali.");
}

static AppointmentService NewService()
{
    return new AppointmentService(new InMemoryAppointmentRepository(), now: () => new DateTime(2026, 6, 29, 10, 0, 0));
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

internal sealed class FakeChatCompletionClient : IChatCompletionClient
{
    private readonly string _response;

    public FakeChatCompletionClient(string response)
    {
        _response = response;
    }

    public Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_response);
    }
}
