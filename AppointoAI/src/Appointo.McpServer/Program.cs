using Appointo.Core;
using Appointo.Tools;

var repository = new InMemoryAppointmentRepository();
var appointmentService = new AppointmentService(repository);
var gateway = new ToolGateway(appointmentService);

Console.WriteLine("Appointo AI local MCP boundary");
Console.WriteLine("Bu ilk surum gercek MCP transport yerine tool semalarini listeler.");
Console.WriteLine();

foreach (var schema in gateway.GetSchemas())
{
    Console.WriteLine($"{schema.Name}: {schema.Description}");
    Console.WriteLine($"  Required: {string.Join(", ", schema.RequiredFields)}");
}
