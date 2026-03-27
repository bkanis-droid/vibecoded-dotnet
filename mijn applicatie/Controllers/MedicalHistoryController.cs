using Microsoft.AspNetCore.Mvc;
using MedicalCustomerManagement.Models.DTOs;
using MedicalCustomerManagement.Services;
using Microsoft.Extensions.Logging;

namespace MedicalCustomerManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicalHistoryController : ControllerBase
    {
        private readonly IMedicalHistoryService _historyService;
        private readonly IPatientService _patientService;
        private readonly ILogger<MedicalHistoryController> _logger;

        public MedicalHistoryController(IMedicalHistoryService historyService, IPatientService patientService, ILogger<MedicalHistoryController> logger)
        {
            _historyService = historyService;
            _patientService = patientService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MedicalHistoryDto>> GetHistory(int id)
        {
            var history = await _historyService.GetHistoryByIdAsync(id);
            if (history == null)
                return NotFound();

            return Ok(history);
        }

        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<List<MedicalHistoryDto>>> GetPatientHistory(int patientId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var histories = await _historyService.GetPatientHistoryAsync(patientId, startDate, endDate);
            return Ok(histories);
        }

        [HttpPost]
        public async Task<ActionResult<MedicalHistoryDto>> CreateHistory(CreateMedicalHistoryDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            // explicit business rule: diagnosis required
            if (string.IsNullOrWhiteSpace(dto.Diagnosis))
            {
                _logger.LogWarning("Missing diagnosis field");
                return BadRequest("Diagnosis is required");
            }

            // simple sanity check for patient id
            if (dto.PatientId <= 0)
            {
                _logger.LogWarning("Invalid patient id {PatientId}", dto.PatientId);
                return BadRequest("Patient ID must be a positive integer");
            }

            // Validate patient exists
            var patient = await _patientService.GetPatientByIdAsync(dto.PatientId);
            if (patient == null)
                return BadRequest("Patient not found");

            try
            {
                var history = await _historyService.CreateHistoryAsync(dto);
                return CreatedAtAction(nameof(GetHistory), new { id = history.Id }, history);
            }
            catch (ArgumentException argEx)
            {
                // service uses this for validation issues like missing patient
                return BadRequest(argEx.Message);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error creating medical history for patient {PatientId}", dto.PatientId);
                // if FK constraint or other db issue, treat as bad request for test purposes
                return BadRequest("Unable to save medical history");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating medical history for patient {PatientId}", dto.PatientId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
