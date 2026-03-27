using System.ComponentModel.DataAnnotations;

namespace MedicalCustomerManagement.Models
{
    /// <summary>
    /// Medical History entity for storing immutable medical records
    /// </summary>
    public class MedicalHistory
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        public int? DoctorId { get; set; }

        [Required]
        public DateTime VisitDate { get; set; }

        [MaxLength(500)]
        public string? Diagnosis { get; set; }

        [MaxLength(1000)]
        public string? Treatment { get; set; }

        [MaxLength(500)]
        public string? Symptoms { get; set; }

        [MaxLength(500)]
        public string? Medications { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false; // Soft delete for compliance

        // Navigation properties
        public Patient? Patient { get; set; }
    }
}
