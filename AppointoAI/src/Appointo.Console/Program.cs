using Appointo.Agent;
using Appointo.Core;
using Appointo.Tools;

var repository = new InMemoryAppointmentRepository();
var appointmentService = new AppointmentService(repository);
var gateway = new ToolGateway(appointmentService);
var parser = new StructuredAppointmentParser();
var agent = new AppointmentAgent(parser, gateway);
var state = new ConversationState();

Console.WriteLine("Appointo AI");
Console.WriteLine("Cikmak icin 'exit' yazin.");
Console.WriteLine("Structured output gormek icin: /parse <mesaj>");
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
        var parsed = parser.Parse(message, DateOnly.FromDateTime(DateTime.Now));
        Console.WriteLine(StructuredOutputFormatter.ToJson(parsed));
        Console.WriteLine();
        continue;
    }

    var response = await agent.HandleAsync(input, state, UserContext.Guest);
    Console.WriteLine($"Appointo AI: {response}");
    Console.WriteLine();
}
