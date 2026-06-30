using Appointo.Core;
using Appointo.Tools;

var repository = new InMemoryAppointmentRepository();
var appointmentService = new AppointmentService(repository, now: () => new DateTime(2026, 6, 29, 10, 0, 0));
var logger = new InMemoryToolExecutionLogger();
var gateway = new ToolGateway(appointmentService, logger: logger);

Console.WriteLine("Appointo AI local MCP boundary");
Console.WriteLine("Bu ekran artik sadece schema listelemiyor; ornek tool cagrilarini da calistiriyor.");
Console.WriteLine("Komutlar: schemas, demo-create, demo-slots, demo-denied-cancel, logs, help, exit");
Console.WriteLine();

while (true)
{
    Console.Write("mcp> ");
    var input = Console.ReadLine()?.Trim().ToLowerInvariant();

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    if (input == "exit")
    {
        break;
    }

    switch (input)
    {
        case "schemas":
            PrintSchemas(gateway);
            break;
        case "demo-create":
            await RunCreateDemoAsync(gateway);
            break;
        case "demo-slots":
            await RunSlotDemoAsync(gateway);
            break;
        case "demo-denied-cancel":
            await RunDeniedCancelDemoAsync(gateway);
            break;
        case "logs":
            PrintLogs(gateway.GetLogs());
            break;
        case "help":
            PrintHelp();
            break;
        default:
            Console.WriteLine("Bilinmeyen komut. Yardim icin 'help' yazabilirsin.");
            Console.WriteLine();
            break;
    }
}

static void PrintSchemas(ToolGateway gateway)
{
    foreach (var schema in gateway.GetSchemas())
    {
        Console.WriteLine($"{schema.Name}: {schema.Description}");
        Console.WriteLine($"  Required: {string.Join(", ", schema.RequiredFields)}");
    }

    Console.WriteLine();
}

static async Task RunCreateDemoAsync(ToolGateway gateway)
{
    Console.WriteLine("Senaryo: Guest kullanici yeni bir randevu olusturuyor.");

    var request = new CreateAppointmentToolRequest(
        "Ahmet Kaya",
        "0555 111 22 33",
        "dis muayenesi",
        new DateOnly(2026, 6, 30),
        new TimeOnly(14, 0),
        "MCP demo akisi");

    var result = await gateway.ExecuteAsync(AppointmentToolNames.CreateAppointment, request, UserContext.Guest);
    Console.WriteLine($"Tool: {AppointmentToolNames.CreateAppointment}");
    Console.WriteLine($"Result: {(result.Success ? "SUCCESS" : "FAIL")}");
    Console.WriteLine($"Message: {result.Message}");

    if (result.Payload is Appointment appointment)
    {
        Console.WriteLine($"AppointmentId: {appointment.Id}");
    }

    Console.WriteLine();
}

static async Task RunSlotDemoAsync(ToolGateway gateway)
{
    Console.WriteLine("Senaryo: Kullanici ogleden sonra ilk uygun slotlari soruyor.");

    var request = new FindNextAvailableSlotToolRequest(
        new DateOnly(2026, 6, 30),
        "danismanlik",
        "afternoon");

    var result = await gateway.ExecuteAsync(AppointmentToolNames.FindNextAvailableSlot, request, UserContext.Guest);
    Console.WriteLine($"Tool: {AppointmentToolNames.FindNextAvailableSlot}");
    Console.WriteLine($"Result: {(result.Success ? "SUCCESS" : "FAIL")}");
    Console.WriteLine($"Message: {result.Message}");
    Console.WriteLine();
}

static async Task RunDeniedCancelDemoAsync(ToolGateway gateway)
{
    Console.WriteLine("Senaryo: Guest kullanici iptal tool'unu cagiriyor.");

    var request = new CancelAppointmentToolRequest(Guid.NewGuid());
    var result = await gateway.ExecuteAsync(AppointmentToolNames.CancelAppointment, request, UserContext.Guest);

    Console.WriteLine($"Tool: {AppointmentToolNames.CancelAppointment}");
    Console.WriteLine($"Result: {(result.Success ? "SUCCESS" : "FAIL")}");
    Console.WriteLine($"Message: {result.Message}");
    Console.WriteLine();
}

static void PrintLogs(IReadOnlyList<ToolExecutionLogEntry> entries)
{
    if (entries.Count == 0)
    {
        Console.WriteLine("Henuz log yok.");
        Console.WriteLine();
        return;
    }

    foreach (var entry in entries)
    {
        Console.WriteLine($"[{entry.TimestampUtc:yyyy-MM-dd HH:mm:ss}] stage={entry.Stage} tool={entry.ToolName} role={entry.Role} success={entry.Success} message=\"{entry.Message}\"");
    }

    Console.WriteLine();
}

static void PrintHelp()
{
    Console.WriteLine("schemas            -> Tool semalarini listeler");
    Console.WriteLine("demo-create        -> Ornek create_appointment cagrisi yapar");
    Console.WriteLine("demo-slots         -> Ornek find_next_available_slot cagrisi yapar");
    Console.WriteLine("demo-denied-cancel -> Yetki reddi ornegini gosterir");
    Console.WriteLine("logs               -> Tool gateway loglarini gosterir");
    Console.WriteLine("exit               -> Programdan cikar");
    Console.WriteLine();
}
