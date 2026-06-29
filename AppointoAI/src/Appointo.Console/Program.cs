using Appointo.Agent;
using Appointo.Core;
using Appointo.Tools;

var repository = new InMemoryAppointmentRepository();
var appointmentService = new AppointmentService(repository);
var gateway = new ToolGateway(appointmentService);
var agent = new AppointmentAgent(new StructuredAppointmentParser(), gateway);
var state = new ConversationState();

Console.WriteLine("Appointo AI");
Console.WriteLine("Cikmak icin 'exit' yazin.");
Console.WriteLine();

while (true)
{
    Console.Write("Siz: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    var response = await agent.HandleAsync(input, state, UserContext.Guest);
    Console.WriteLine($"Appointo AI: {response}");
    Console.WriteLine();
}
