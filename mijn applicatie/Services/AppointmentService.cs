using Microsoft.EntityFrameworkCore;
using MedicalCustomerManagement.Models;
using MedicalCustomerManagement.Models.DTOs;

namespace MedicalCustomerManagement.Services
{
    public interface IAppointmentService
    {
        Task<AppointmentDto?> GetAppointmentByIdAsync(int id);
        Task<List<AppointmentDto>> GetPatientAppointmentsAsync(int patientId);
        Task<List<AppointmentDto>> GetAppointmentsForDateAsync(DateTime date);
        Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto dto);
        Task<AppointmentDto?> UpdateAppointmentAsync(int id, UpdateAppointmentDto dto);
        Task<bool> DeleteAppointmentAsync(int id);
        Task<bool> CheckConflictAsync(int patientId, DateTime appointmentDate);
    }

    public class AppointmentService : IAppointmentService
    {
        private readonly Data.MedicalDbContext _context;
        private readonly ILogger<AppointmentService> _logger;
        private readonly IAuditService _auditService;

        public AppointmentService(Data.MedicalDbContext context, ILogger<AppointmentService> logger, IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<AppointmentDto?> GetAppointmentByIdAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            return appointment == null ? null : MapToDto(appointment);
        }

        public async Task<List<AppointmentDto>> GetPatientAppointmentsAsync(int patientId)
        {
            var appointments = await _context.Appointments
                .Where(a => a.PatientId == patientId && a.Status != "Cancelled")
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return appointments.Select(MapToDto).ToList();
        }

        public async Task<List<AppointmentDto>> GetAppointmentsForDateAsync(DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            // exclude cancelled appointments so they don’t show up on the daily schedule
            var appointments = await _context.Appointments
                .Where(a => a.AppointmentDate >= startOfDay && a.AppointmentDate < endOfDay && a.Status != "Cancelled")
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            return appointments.Select(MapToDto).ToList();
        }

        public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto dto)
        {
            // verify patient exists - this helps avoid FK exceptions later
            var patientExists = await _context.Patients.AnyAsync(p => p.Id == dto.PatientId && !p.IsDeleted);
            if (!patientExists)
                throw new ArgumentException("Patient not found");

            // Check for conflicts
            if (await CheckConflictAsync(dto.PatientId, dto.AppointmentDate))
                throw new InvalidOperationException("Appointment conflict detected for this patient.");

            var appointment = new Appointment
            {
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                AppointmentDate = dto.AppointmentDate,
                Reason = dto.Reason,
                Status = "Scheduled",
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Appointment {AppointmentId} created for patient {PatientId}", appointment.Id, dto.PatientId);

            // Log audit trail
            await _auditService.LogActionAsync(
                dto.PatientId,
                "CREATE",
                "Appointment",
                null,
                System.Text.Json.JsonSerializer.Serialize(new { appointment.AppointmentDate, appointment.Reason, appointment.Status }),
                null
            );

            return MapToDto(appointment);
        }

        public async Task<AppointmentDto?> UpdateAppointmentAsync(int id, UpdateAppointmentDto dto)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return null;

            // Capture old values for audit trail
            var oldValues = System.Text.Json.JsonSerializer.Serialize(new 
            { 
                appointment.AppointmentDate, 
                appointment.Reason, 
                appointment.Status,
                appointment.Notes
            });

            if (dto.AppointmentDate.HasValue)
            {
                if (await CheckConflictAsync(appointment.PatientId, dto.AppointmentDate.Value))
                    throw new InvalidOperationException("Appointment conflict detected for this patient.");

                appointment.AppointmentDate = dto.AppointmentDate.Value;
            }

            if (!string.IsNullOrEmpty(dto.Reason))
                appointment.Reason = dto.Reason;
            if (!string.IsNullOrEmpty(dto.Status))
                appointment.Status = dto.Status;
            if (!string.IsNullOrEmpty(dto.Notes))
                appointment.Notes = dto.Notes;

            appointment.UpdatedAt = DateTime.UtcNow;

            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Appointment {AppointmentId} updated", id);

            // Capture new values for audit trail
            var newValues = System.Text.Json.JsonSerializer.Serialize(new 
            { 
                appointment.AppointmentDate, 
                appointment.Reason, 
                appointment.Status,
                appointment.Notes
            });

            // Log audit trail
            await _auditService.LogActionAsync(
                appointment.PatientId,
                "UPDATE",
                "Appointment",
                oldValues,
                newValues,
                null
            );

            return MapToDto(appointment);
        }

        public async Task<bool> DeleteAppointmentAsync(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
                return false;

            appointment.Status = "Cancelled";
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Appointment {AppointmentId} cancelled", id);

            // Log audit trail
            await _auditService.LogActionAsync(
                appointment.PatientId,
                "DELETE",
                "Appointment",
                System.Text.Json.JsonSerializer.Serialize(new { appointment.AppointmentDate, appointment.Reason }),
                null,
                null
            );

            return true;
        }

        public async Task<bool> CheckConflictAsync(int patientId, DateTime appointmentDate)
        {
            var conflict = await _context.Appointments
                .AnyAsync(a => a.PatientId == patientId 
                    && a.AppointmentDate.Date == appointmentDate.Date 
                    && a.AppointmentDate.Hour == appointmentDate.Hour 
                    && a.AppointmentDate.Minute == appointmentDate.Minute
                    && a.Status != "Cancelled");

            return conflict;
        }

        private static AppointmentDto MapToDto(Appointment appointment)
        {
            return new AppointmentDto
            {
                Id = appointment.Id,
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                AppointmentDate = appointment.AppointmentDate,
                Reason = appointment.Reason,
                Status = appointment.Status,
                Notes = appointment.Notes
            };
        }
    }
}
