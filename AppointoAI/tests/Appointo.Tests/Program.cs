using Appointo.Agent;
using Appointo.Core;
using Appointo.Tools;

var tests = new List<(string Name, Func<Task> Run)>
{
    ("Intent parser detects create appointment", IntentParserDetectsCreateAppointment),
    ("Appointment service rejects lunch break", AppointmentServiceRejectsLunchBreak),
    ("Appointment service rejects overlapping slot", AppointmentServiceRejectsOverlappingSlot),
    ("Permission matrix blocks guest cancellation", PermissionMatrixBlocksGuestCancellation),
    ("Agent asks missing details", AgentAsksMissingDetails)
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
    var agent = new AppointmentAgent(new StructuredAppointmentParser(), new ToolGateway(NewService()), () => new DateOnly(2026, 6, 29));
    var response = await agent.HandleAsync("Yarin randevu almak istiyorum.", new ConversationState(), UserContext.Guest);
    Assert(response.Contains("ad soyad") || response.Contains("Hangi hizmet"), "Agent eksik bilgi sormali.");
}

static AppointmentService NewService()
{
    return new AppointmentService(new InMemoryAppointmentRepository(), now: () => new DateTime(2026, 6, 29, 10, 0, 0));
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
