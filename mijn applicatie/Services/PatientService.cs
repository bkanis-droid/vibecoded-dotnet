using MedicalCustomerManagement.Models;
using MedicalCustomerManagement.Models.DTOs;

namespace MedicalCustomerManagement.Services
{
    public interface IPatientService
    {
        Task<PatientDto?> GetPatientByIdAsync(int id);
        Task<List<PatientDto>> GetAllPatientsAsync();
        Task<PatientDto> CreatePatientAsync(CreatePatientDto dto);
        Task<PatientDto?> UpdatePatientAsync(int id, UpdatePatientDto dto);
        Task<bool> DeletePatientAsync(int id);
    }

    public class PatientService : IPatientService
    {
        private readonly Data.MedicalDbContext _context;
        private readonly ILogger<PatientService> _logger;
        private readonly IAuditService _auditService;

        public PatientService(Data.MedicalDbContext context, ILogger<PatientService> logger, IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<PatientDto?> GetPatientByIdAsync(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null || patient.IsDeleted)
                return null;

            return MapToDto(patient);
        }

        public async Task<List<PatientDto>> GetAllPatientsAsync()
        {
            var patients = await Task.FromResult(_context.Patients
                .Where(p => !p.IsDeleted && p.IsActive)
                .ToList());

            return patients.Select(MapToDto).ToList();
        }

        public async Task<PatientDto> CreatePatientAsync(CreatePatientDto dto)
        {
            _logger.LogInformation("Creating new patient {FirstName} {LastName}", dto.FirstName, dto.LastName);
            var patient = new Patient
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                Address = dto.Address,
                City = dto.City,
                PostalCode = dto.PostalCode,
                MedicalInsuranceNumber = dto.MedicalInsuranceNumber,
                CreatedAt = DateTime.UtcNow
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Patient {PatientId} created", patient.Id);
            
            // Log audit trail
            await _auditService.LogActionAsync(
                patient.Id,
                "CREATE",
                "Patient",
                null,
                System.Text.Json.JsonSerializer.Serialize(new { patient.FirstName, patient.LastName, patient.Email }),
                null
            );

            return MapToDto(patient);
        }

        public async Task<PatientDto?> UpdatePatientAsync(int id, UpdatePatientDto dto)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null || patient.IsDeleted)
                return null;

            // Capture old values for audit trail
            var oldValues = System.Text.Json.JsonSerializer.Serialize(new 
            { 
                patient.FirstName, 
                patient.LastName, 
                patient.Email,
                patient.PhoneNumber,
                patient.Gender,
                patient.Address,
                patient.City,
                patient.PostalCode
            });

            if (!string.IsNullOrEmpty(dto.FirstName))
                patient.FirstName = dto.FirstName;
            if (!string.IsNullOrEmpty(dto.LastName))
                patient.LastName = dto.LastName;
            if (!string.IsNullOrEmpty(dto.Email))
                patient.Email = dto.Email;
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
                patient.PhoneNumber = dto.PhoneNumber;
            if (!string.IsNullOrEmpty(dto.Gender))
                patient.Gender = dto.Gender;
            if (!string.IsNullOrEmpty(dto.Address))
                patient.Address = dto.Address;
            if (!string.IsNullOrEmpty(dto.City))
                patient.City = dto.City;
            if (!string.IsNullOrEmpty(dto.PostalCode))
                patient.PostalCode = dto.PostalCode;

            patient.UpdatedAt = DateTime.UtcNow;

            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();

            // Capture new values for audit trail
            var newValues = System.Text.Json.JsonSerializer.Serialize(new 
            { 
                patient.FirstName, 
                patient.LastName, 
                patient.Email,
                patient.PhoneNumber,
                patient.Gender,
                patient.Address,
                patient.City,
                patient.PostalCode
            });

            // Log audit trail
            await _auditService.LogActionAsync(
                patient.Id,
                "UPDATE",
                "Patient",
                oldValues,
                newValues,
                null
            );

            _logger.LogInformation("Patient {PatientId} updated", id);
            return MapToDto(patient);
        }

        public async Task<bool> DeletePatientAsync(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
                return false;

            patient.IsDeleted = true;
            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Patient {PatientId} deleted", id);

            // Log audit trail
            await _auditService.LogActionAsync(
                patient.Id,
                "DELETE",
                "Patient",
                System.Text.Json.JsonSerializer.Serialize(new { patient.FirstName, patient.LastName, patient.Email }),
                null,
                null
            );

            return true;
        }

        private static PatientDto MapToDto(Patient patient)
        {
            return new PatientDto
            {
                Id = patient.Id,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                PhoneNumber = patient.PhoneNumber,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                IsActive = patient.IsActive
            };
        }
    }
}
