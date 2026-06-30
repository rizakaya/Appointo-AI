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
    ("Tool gateway logs successful execution", ToolGatewayLogsSuccessfulExecution),
    ("Tool gateway logs denied execution", ToolGatewayLogsDeniedExecution),
    ("Agent asks missing details", AgentAsksMissingDetails),
    ("Agent completes appointment across turns", AgentCompletesAppointmentAcrossTurns),
    ("Agent answers service info from knowledge base", AgentAnswersServiceInfoFromKnowledgeBase),
    ("Agent answers working hours from knowledge base", AgentAnswersWorkingHoursFromKnowledgeBase),
    ("Agent answers cancellation policy from knowledge base", AgentAnswersCancellationPolicyFromKnowledgeBase),
    ("Structured output formats parse result", StructuredOutputFormatsParseResult),
    ("Ollama parser maps JSON response", OllamaParserMapsJsonResponse),
    ("Ollama parser falls back on invalid JSON", OllamaParserFallsBackOnInvalidJson),
    ("Handoff routes missing customer info to customer agent", HandoffRoutesMissingCustomerInfoToCustomerAgent),
    ("Handoff routes availability question to availability agent", HandoffRoutesAvailabilityQuestionToAvailabilityAgent),
    ("Handoff routes unknown request to support agent", HandoffRoutesUnknownRequestToSupportAgent)
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
    var result = parser.Parse("Yarin saat 14:00 icin Cagla Sahin adina sac kesim randevusu olustur. 0555 333 44 55", new DateOnly(2026, 6, 29));
    var json = StructuredOutputFormatter.ToJson(result);

    Assert(result.Intent == AppointmentIntent.CreateAppointment, "ASCII guvenli intent create olmali.");
    Assert(result.CustomerName == "Cagla Sahin", "Musteri adi bulunmali.");
    Assert(result.ServiceType == "sac kesim", "Hizmet normalize edilmeli.");
    Assert(json.Contains("Cagla Sahin"), "JSON musteri adini yazmali.");
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

static async Task ToolGatewayLogsSuccessfulExecution()
{
    var logger = new InMemoryToolExecutionLogger();
    var gateway = new ToolGateway(NewService(), logger: logger);

    var result = await gateway.ExecuteAsync(
        AppointmentToolNames.FindNextAvailableSlot,
        new FindNextAvailableSlotToolRequest(new DateOnly(2026, 6, 30), "danismanlik", "afternoon"),
        UserContext.Guest);

    Assert(result.Success, "Demo slot sorgusu basarili olmali.");
    Assert(logger.GetEntries().Count == 2, "Basarili cagrida attempt ve completed loglari olusmali.");
    Assert(logger.GetEntries()[0].Stage == "Attempt", "Ilk log attempt olmali.");
    Assert(logger.GetEntries()[1].Stage == "Completed", "Ikinci log completed olmali.");
}

static async Task ToolGatewayLogsDeniedExecution()
{
    var logger = new InMemoryToolExecutionLogger();
    var gateway = new ToolGateway(NewService(), logger: logger);

    var result = await gateway.ExecuteAsync(
        AppointmentToolNames.CancelAppointment,
        new CancelAppointmentToolRequest(Guid.NewGuid()),
        UserContext.Guest);

    Assert(!result.Success, "Guest iptal yetkisi olmamali.");
    Assert(logger.GetEntries().Count == 2, "Reddedilen cagrida attempt ve denied loglari olusmali.");
    Assert(logger.GetEntries()[1].Stage == "Denied", "Ikinci log denied olmali.");
}

static async Task AgentAsksMissingDetails()
{
    var agent = new AppointmentAgent(new RuleBasedAppointmentIntentParser(), new ToolGateway(NewService()), today: () => new DateOnly(2026, 6, 29));
    var response = await agent.HandleAsync("Yarin randevu almak istiyorum.", new ConversationState(), UserContext.Guest);
    Assert(response.Contains("ad soyad") || response.Contains("Hangi hizmet"), "Agent eksik bilgi sormali.");
}

static async Task AgentCompletesAppointmentAcrossTurns()
{
    var state = new ConversationState();
    var agent = new AppointmentAgent(new RuleBasedAppointmentIntentParser(), new ToolGateway(NewService()), today: () => new DateOnly(2026, 6, 29));

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

static async Task AgentAnswersServiceInfoFromKnowledgeBase()
{
    var agent = new AppointmentAgent(
        new RuleBasedAppointmentIntentParser(),
        new ToolGateway(NewService()),
        new StubKnowledgeBase("Hizmet Bilgileri:\n| Sac kesim | 45 dakika |"),
        () => new DateOnly(2026, 6, 29));

    var response = await agent.HandleAsync("Sac kesim ne kadar surer?", new ConversationState(), UserContext.Guest);

    Assert(response.Contains("Hizmet Bilgileri"), "Bilgi sorusu knowledge base'ten cevaplanmali.");
    Assert(response.Contains("45 dakika"), "Hizmet suresi bilgi tabanindan gelmeli.");
}

static async Task AgentAnswersWorkingHoursFromKnowledgeBase()
{
    var agent = new AppointmentAgent(
        new RuleBasedAppointmentIntentParser(),
        new ToolGateway(NewService()),
        new StubKnowledgeBase("Calisma Saatleri:\n- Calisma saatleri: 09:00 - 18:00"),
        () => new DateOnly(2026, 6, 29));

    var response = await agent.HandleAsync("Kacta acik, calisma saatleriniz nedir?", new ConversationState(), UserContext.Guest);

    Assert(response.Contains("Calisma Saatleri"), "Calisma saati sorusu knowledge base'ten cevaplanmali.");
    Assert(response.Contains("09:00 - 18:00"), "Calisma saatleri bilgi tabanindan gelmeli.");
}

static async Task AgentAnswersCancellationPolicyFromKnowledgeBase()
{
    var agent = new AppointmentAgent(
        new RuleBasedAppointmentIntentParser(),
        new ToolGateway(NewService()),
        new StubKnowledgeBase("Iptal Politikasi:\n- Randevu iptali, randevu saatinden en az 2 saat once yapilabilir."),
        () => new DateOnly(2026, 6, 29));

    var response = await agent.HandleAsync("Randevuyu ne zamana kadar iptal edebilirim?", new ConversationState(), UserContext.Guest);

    Assert(response.Contains("Iptal Politikasi"), "Iptal sorusu knowledge base'ten cevaplanmali.");
    Assert(response.Contains("2 saat"), "Iptal kurali bilgi tabanindan gelmeli.");
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

static async Task HandoffRoutesMissingCustomerInfoToCustomerAgent()
{
    var router = new HandoffRouter(new RuleBasedAppointmentIntentParser(), () => new DateOnly(2026, 6, 29));

    var decision = await router.DecideAsync("Yarin saat 14:00 icin sac kesim randevusu olustur.");

    Assert(decision.TargetAgent == "CustomerAgent", "Eksik musteri bilgisi CustomerAgent'a gitmeli.");
}

static async Task HandoffRoutesAvailabilityQuestionToAvailabilityAgent()
{
    var router = new HandoffRouter(new RuleBasedAppointmentIntentParser(), () => new DateOnly(2026, 6, 29));

    var decision = await router.DecideAsync("Cuma ogleden sonra bosluk var mi?");

    Assert(decision.TargetAgent == "AvailabilityAgent", "Musaitlik sorusu AvailabilityAgent'a gitmeli.");
}

static async Task HandoffRoutesUnknownRequestToSupportAgent()
{
    var router = new HandoffRouter(new RuleBasedAppointmentIntentParser(), () => new DateOnly(2026, 6, 29));

    var decision = await router.DecideAsync("Bana biraz yardim eder misin?");

    Assert(decision.TargetAgent == "SupportAgent", "Bilinmeyen istek SupportAgent'a gitmeli.");
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

internal sealed class StubKnowledgeBase : IRagKnowledgeBase
{
    private readonly string _response;

    public StubKnowledgeBase(string response)
    {
        _response = response;
    }

    public Task<string> AnswerAsync(string question, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_response);
    }
}
