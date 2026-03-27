using System.Text.Json;
using MedicalCustomerManagement.Models;
using MedicalCustomerManagement.Data;

namespace MedicalCustomerManagement.Services
{
    public interface IAuditService
    {
        Task LogActionAsync(int? patientId, string action, string entityType, string? oldValues = null, string? newValues = null, string? ipAddress = null);
    }

    public class AuditService : IAuditService
    {
        private readonly MedicalDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(MedicalDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogActionAsync(int? patientId, string action, string entityType, string? oldValues = null, string? newValues = null, string? ipAddress = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    PatientId = patientId,
                    Action = action,
                    EntityType = entityType,
                    OldValues = oldValues,
                    NewValues = newValues,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ipAddress
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Audit log recorded for PatientId {PatientId}: {Action} on {EntityType}", patientId, action, entityType);
            }
            catch (Exception ex)
            {
                // Log audit failures but don't crash the app
                _logger.LogError(ex, "Failed to record audit log for PatientId {PatientId}", patientId);
            }
        }
    }
}
