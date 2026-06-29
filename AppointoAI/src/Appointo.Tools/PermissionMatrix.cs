namespace Appointo.Tools;

public sealed class PermissionMatrix
{
    public bool CanExecute(string toolName, UserContext user)
    {
        return toolName switch
        {
            AppointmentToolNames.CheckAvailability => true,
            AppointmentToolNames.FindNextAvailableSlot => true,
            AppointmentToolNames.CreateAppointment => true,
            AppointmentToolNames.CancelAppointment => user.Role is UserRole.VerifiedCustomer or UserRole.Staff or UserRole.Admin,
            AppointmentToolNames.RescheduleAppointment => user.Role is UserRole.VerifiedCustomer or UserRole.Staff or UserRole.Admin,
            AppointmentToolNames.FindCustomerAppointments => user.Role is UserRole.VerifiedCustomer or UserRole.Staff or UserRole.Admin,
            _ => false
        };
    }
}
