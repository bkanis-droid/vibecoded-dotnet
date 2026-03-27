using Microsoft.AspNetCore.Mvc;
using MedicalCustomerManagement.Models.DTOs;
using MedicalCustomerManagement.Services;

namespace MedicalCustomerManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IPatientService _patientService;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(IAppointmentService appointmentService, IPatientService patientService, ILogger<AppointmentsController> logger)
        {
            _appointmentService = appointmentService;
            _patientService = patientService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AppointmentDto>> GetAppointment(int id)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
                return NotFound();

            return Ok(appointment);
        }

        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<List<AppointmentDto>>> GetPatientAppointments(int patientId)
        {
            var appointments = await _appointmentService.GetPatientAppointmentsAsync(patientId);
            return Ok(appointments);
        }

        [HttpGet("date/{date}")]
        public async Task<ActionResult<List<AppointmentDto>>> GetAppointmentsByDate(DateTime date)
        {
            var appointments = await _appointmentService.GetAppointmentsForDateAsync(date);
            return Ok(appointments);
        }

        [HttpPost]
        public async Task<ActionResult<AppointmentDto>> CreateAppointment(CreateAppointmentDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errs = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("ModelState invalid: {Errors}", errs);
                return BadRequest(ModelState);
            }

            if (dto.PatientId <= 0)
            {
                _logger.LogWarning("Invalid patient id provided: {PatientId}", dto.PatientId);
                return BadRequest("Patient ID must be a positive integer");
            }

            // Validate patient exists
            var patient = await _patientService.GetPatientByIdAsync(dto.PatientId);
            if (patient == null)
                return BadRequest("Patient not found");

            try
            {
                var appointment = await _appointmentService.CreateAppointmentAsync(dto);
                return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, appointment);
            }
            catch (ArgumentException ex)
            {
                // used by service for validation issues such as missing patient
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error creating appointment for patient {PatientId}", dto.PatientId);
                return BadRequest("Unable to save appointment");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<AppointmentDto>> UpdateAppointment(int id, UpdateAppointmentDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errs = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("ModelState invalid: {Errors}", errs);
                return BadRequest(ModelState);
            }

            try
            {
                var appointment = await _appointmentService.UpdateAppointmentAsync(id, dto);
                if (appointment == null)
                    return NotFound();

                return Ok(appointment);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAppointment(int id)
        {
            var result = await _appointmentService.DeleteAppointmentAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
}
