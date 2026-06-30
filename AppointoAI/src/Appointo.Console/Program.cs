using Appointo.Agent;
using Appointo.Core;
using Appointo.Tools;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var repository = new InMemoryAppointmentRepository();
var appointmentService = new AppointmentService(repository);
var toolLogger = new InMemoryToolExecutionLogger();
var gateway = new ToolGateway(appointmentService, logger: toolLogger);
var parser = SelectParser();
var knowledgeBase = new FileRagKnowledgeBase(AppContext.BaseDirectory);
var agent = new AppointmentAgent(parser, gateway, knowledgeBase);
var handoffRouter = new HandoffRouter(parser);
var state = new ConversationState();

Console.WriteLine("Appointo AI");
Console.WriteLine("Cikmak icin 'exit' yazin.");
Console.WriteLine($"Parser modu: {(parser is OllamaAppointmentIntentParser ? "ollama" : "rule-based")}");
Console.WriteLine("Structured output gormek icin: /parse <mesaj>");
Console.WriteLine("Handoff kararini gormek icin: /handoff <mesaj>");
Console.WriteLine("Tool semalarini gormek icin: /tools");
Console.WriteLine("Tool loglarini gormek icin: /logs");
Console.WriteLine("Tool demolarini calistirmak icin: /demo-tool create veya /demo-tool denied-cancel");
Console.WriteLine();

while (true)
{
    Console.Write("Siz: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    if (input.StartsWith("/parse ", StringComparison.OrdinalIgnoreCase))
    {
        var message = input["/parse ".Length..];
        var parsed = await parser.ParseAsync(message, DateOnly.FromDateTime(DateTime.Now));
        Console.WriteLine(StructuredOutputFormatter.ToJson(parsed));
        Console.WriteLine();
        continue;
    }

    if (input.StartsWith("/handoff ", StringComparison.OrdinalIgnoreCase))
    {
        var message = input["/handoff ".Length..];
        var decision = await handoffRouter.DecideAsync(message);
        Console.WriteLine($"Target Agent: {decision.TargetAgent}");
        Console.WriteLine($"Reason: {decision.Reason}");
        Console.WriteLine();
        continue;
    }

    if (input.Equals("/tools", StringComparison.OrdinalIgnoreCase))
    {
        PrintToolSchemas(gateway);
        continue;
    }

    if (input.Equals("/logs", StringComparison.OrdinalIgnoreCase))
    {
        PrintToolLogs(gateway.GetLogs());
        continue;
    }

    if (input.StartsWith("/demo-tool ", StringComparison.OrdinalIgnoreCase))
    {
        var demoName = input["/demo-tool ".Length..].Trim();
        await RunDemoToolAsync(demoName, gateway);
        continue;
    }

    var response = await agent.HandleAsync(input, state, UserContext.Guest);
    Console.WriteLine($"Appointo AI: {response}");
    Console.WriteLine();
}

static IAppointmentIntentParser SelectParser()
{
    var ruleBasedParser = new RuleBasedAppointmentIntentParser();

    Console.WriteLine("Appointo AI parser secimi");
    Console.WriteLine("1. Default parser");
    Console.WriteLine("2. Ollama parser");
    Console.Write("Seciminiz (1/2, default 1): ");

    var selection = Console.ReadLine();
    Console.WriteLine();

    return selection?.Trim() == "2"
        ? new OllamaAppointmentIntentParser(new OllamaChatClient(new HttpClient()), ruleBasedParser)
        : ruleBasedParser;
}

static void PrintToolSchemas(ToolGateway gateway)
{
    Console.WriteLine("Tool semalari:");
    foreach (var schema in gateway.GetSchemas())
    {
        Console.WriteLine($"- {schema.Name}: {schema.Description}");
        Console.WriteLine($"  Required: {string.Join(", ", schema.RequiredFields)}");
    }

    Console.WriteLine();
}

static void PrintToolLogs(IReadOnlyList<ToolExecutionLogEntry> entries)
{
    if (entries.Count == 0)
    {
        Console.WriteLine("Henuz tool logu yok. Once bir randevu akisi veya /demo-tool komutu calistir.");
        Console.WriteLine();
        return;
    }

    Console.WriteLine("Tool loglari:");
    foreach (var entry in entries)
    {
        Console.WriteLine($"[{entry.TimestampUtc:yyyy-MM-dd HH:mm:ss}] stage={entry.Stage} tool={entry.ToolName} role={entry.Role} success={entry.Success} message=\"{entry.Message}\"");
    }

    Console.WriteLine();
}

static async Task RunDemoToolAsync(string demoName, ToolGateway gateway)
{
    if (demoName.Equals("create", StringComparison.OrdinalIgnoreCase))
    {
        var request = new CreateAppointmentToolRequest(
            "Demo Kullanici",
            "0555 000 11 22",
            "danismanlik",
            new DateOnly(2026, 7, 2),
            new TimeOnly(10, 0),
            "Console tool demo");

        var result = await gateway.ExecuteAsync(AppointmentToolNames.CreateAppointment, request, UserContext.Guest);
        PrintToolResult(AppointmentToolNames.CreateAppointment, result);
        return;
    }

    if (demoName.Equals("denied-cancel", StringComparison.OrdinalIgnoreCase))
    {
        var request = new CancelAppointmentToolRequest(Guid.NewGuid());
        var result = await gateway.ExecuteAsync(AppointmentToolNames.CancelAppointment, request, UserContext.Guest);
        PrintToolResult(AppointmentToolNames.CancelAppointment, result);
        return;
    }

    Console.WriteLine("Bilinmeyen demo. Kullanilabilir demolar: /demo-tool create, /demo-tool denied-cancel");
    Console.WriteLine();
}

static void PrintToolResult(string toolName, ToolExecutionResult result)
{
    Console.WriteLine($"Tool: {toolName}");
    Console.WriteLine($"Result: {(result.Success ? "SUCCESS" : "FAIL")}");
    Console.WriteLine($"Message: {result.Message}");

    if (result.Payload is Appointment appointment)
    {
        Console.WriteLine($"AppointmentId: {appointment.Id}");
    }

    Console.WriteLine();
}
