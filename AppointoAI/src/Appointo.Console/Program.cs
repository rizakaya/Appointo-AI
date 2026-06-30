using Appointo.Agent;
using Appointo.Core;
using Appointo.Tools;
using System.Text;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var repository = new InMemoryAppointmentRepository();
var appointmentService = new AppointmentService(repository);
var gateway = new ToolGateway(appointmentService);
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
