using Microsoft.EntityFrameworkCore;
using MedicalCustomerManagement.Models;
using MedicalCustomerManagement.Models.DTOs;

namespace MedicalCustomerManagement.Services
{
    public interface IMedicalHistoryService
    {
        Task<MedicalHistoryDto?> GetHistoryByIdAsync(int id);
        Task<List<MedicalHistoryDto>> GetPatientHistoryAsync(int patientId, DateTime? startDate = null, DateTime? endDate = null);
        Task<MedicalHistoryDto> CreateHistoryAsync(CreateMedicalHistoryDto dto);
    }

    public class MedicalHistoryService : IMedicalHistoryService
    {
        private readonly Data.MedicalDbContext _context;
        private readonly ILogger<MedicalHistoryService> _logger;
        private readonly IAuditService _auditService;

        public MedicalHistoryService(Data.MedicalDbContext context, ILogger<MedicalHistoryService> logger, IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<MedicalHistoryDto?> GetHistoryByIdAsync(int id)
        {
            var history = await _context.MedicalHistories.FindAsync(id);
            if (history == null || history.IsDeleted)
                return null;

            return MapToDto(history);
        }

        public async Task<List<MedicalHistoryDto>> GetPatientHistoryAsync(int patientId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.MedicalHistories
                .Where(m => m.PatientId == patientId && !m.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(m => m.VisitDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(m => m.VisitDate <= endDate.Value);

            var histories = await query
                .OrderByDescending(m => m.VisitDate)
                .ToListAsync();

            return histories.Select(MapToDto).ToList();
        }

        public async Task<MedicalHistoryDto> CreateHistoryAsync(CreateMedicalHistoryDto dto)
        {
            // ensure patient exists to avoid foreign-key violation
            var patientExists = await _context.Patients.AnyAsync(p => p.Id == dto.PatientId && !p.IsDeleted);
            if (!patientExists)
                throw new ArgumentException("Patient not found");

            var history = new MedicalHistory
            {
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                VisitDate = dto.VisitDate,
                Diagnosis = dto.Diagnosis,
                Treatment = dto.Treatment,
                Symptoms = dto.Symptoms,
                Medications = dto.Medications,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.MedicalHistories.Add(history);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Medical history {MedicalHistoryId} created for patient {PatientId}", history.Id, dto.PatientId);

            // Log audit trail
            await _auditService.LogActionAsync(
                dto.PatientId,
                "CREATE",
                "MedicalHistory",
                null,
                System.Text.Json.JsonSerializer.Serialize(new { history.VisitDate, history.Diagnosis, history.Treatment }),
                null
            );

            return MapToDto(history);
        }

        private static MedicalHistoryDto MapToDto(MedicalHistory history)
        {
            return new MedicalHistoryDto
            {
                Id = history.Id,
                PatientId = history.PatientId,
                DoctorId = history.DoctorId,
                VisitDate = history.VisitDate,
                Diagnosis = history.Diagnosis,
                Treatment = history.Treatment,
                Symptoms = history.Symptoms,
                Medications = history.Medications,
                Notes = history.Notes
            };
        }
    }
}
