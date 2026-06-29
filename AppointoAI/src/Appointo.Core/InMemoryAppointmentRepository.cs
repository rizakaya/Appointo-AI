namespace Appointo.Core;

public sealed class InMemoryAppointmentRepository : IAppointmentRepository
{
    private readonly List<Appointment> _appointments = [];

    public Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _appointments.Add(appointment);
        return Task.CompletedTask;
    }

    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_appointments.FirstOrDefault(x => x.Id == id));
    }

    public Task<IReadOnlyList<Appointment>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Appointment>>(_appointments.ToList());
    }
}
