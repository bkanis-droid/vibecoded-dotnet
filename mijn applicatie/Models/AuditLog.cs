namespace MedicalCustomerManagement.Models
{
    /// <summary>
    /// Audit log for HIPAA compliance - tracks all patient data access and modifications
    /// </summary>
    public class AuditLog
    {
        public int Id { get; set; }
        public int? PatientId { get; set; }
        public string Action { get; set; } = string.Empty; // CREATE, READ, UPDATE, DELETE
        public string EntityType { get; set; } = string.Empty; // Patient, Appointment, MedicalHistory
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? OldValues { get; set; } // JSON of previous values
        public string? NewValues { get; set; } // JSON of new values
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
