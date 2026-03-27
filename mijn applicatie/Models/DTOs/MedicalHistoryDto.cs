using System.ComponentModel.DataAnnotations;

namespace MedicalCustomerManagement.Models.DTOs
{
    public class MedicalHistoryDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int? DoctorId { get; set; }
        public DateTime VisitDate { get; set; }
        public string? Diagnosis { get; set; }
        public string? Treatment { get; set; }
        public string? Symptoms { get; set; }
        public string? Medications { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateMedicalHistoryDto
    {
        [Required(ErrorMessage = "Patient ID is required")]
        public int PatientId { get; set; }

        public int? DoctorId { get; set; }

        [Required(ErrorMessage = "Visit date is required")]
        [DataType(DataType.Date)]
        public DateTime VisitDate { get; set; }

        [Required(ErrorMessage = "Diagnosis is required")]
        [StringLength(500, ErrorMessage = "Diagnosis cannot exceed 500 characters")]
        public string Diagnosis { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Treatment cannot exceed 500 characters")]
        public string? Treatment { get; set; }

        [StringLength(1000, ErrorMessage = "Symptoms cannot exceed 1000 characters")]
        public string? Symptoms { get; set; }

        [StringLength(1000, ErrorMessage = "Medications cannot exceed 1000 characters")]
        public string? Medications { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        public string? Notes { get; set; }
    }
}
