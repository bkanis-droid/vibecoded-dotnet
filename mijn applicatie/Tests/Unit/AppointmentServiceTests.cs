using Xunit;
using MedicalCustomerManagement.Services;
using MedicalCustomerManagement.Models;
using MedicalCustomerManagement.Models.DTOs;
using MedicalCustomerManagement.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;

namespace MedicalCustomerManagement.Tests.Unit
{
    public class AppointmentServiceTests
    {
        private (MedicalDbContext context, AppointmentService service) CreateContextAndService()
        {
            var conn = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            conn.Open();
            var options = new DbContextOptionsBuilder<MedicalDbContext>()
                .UseSqlite(conn)
                .Options;
            var context = new MedicalDbContext(options);
            // ensure schema exists for in-memory database
            context.Database.EnsureCreated();
            var mockLogger = new Mock<ILogger<AppointmentService>>();
            var mockAuditService = new Mock<IAuditService>();
            var service = new AppointmentService(context, mockLogger.Object, mockAuditService.Object);
            return (context, service);
        }

        [Fact]
        public async Task CheckConflictAsync_ReturnsTrue_WhenAppointmentExists()
        {
            var (context, service) = CreateContextAndService();
            // Arrange - create patient first
            var patient = new Patient { Id = 1, FirstName = "Test", LastName = "Patient", Email = "test@test.com", IsDeleted = false };
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
            
            var existing = new Appointment
            {
                PatientId = 1,
                AppointmentDate = new DateTime(2024,1,1,10,0,0),
                Status = "Scheduled"
            };
            context.Appointments.Add(existing);
            await context.SaveChangesAsync();

            // Act
            var conflict = await service.CheckConflictAsync(1, existing.AppointmentDate);

            // Assert
            Assert.True(conflict);
        }

        [Fact]
        public async Task CheckConflictAsync_ReturnsFalse_WhenNoAppointmentExists()
        {
            var (context, service) = CreateContextAndService();
            // no data seeded

            // Act
            var conflict = await service.CheckConflictAsync(1, DateTime.Now);

            // Assert
            Assert.False(conflict);
        }

        [Fact]
        public async Task CreateAppointmentAsync_ThrowsException_WhenConflictExists()
        {
            var (context, service) = CreateContextAndService();
            // Arrange - create patient first
            var patient = new Patient { Id = 1, FirstName = "Test", LastName = "Patient", Email = "test@test.com", IsDeleted = false };
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
            
            var existing = new Appointment
            {
                PatientId = 1,
                AppointmentDate = new DateTime(2024,1,1,10,0,0),
                Status = "Scheduled"
            };
            context.Appointments.Add(existing);
            await context.SaveChangesAsync();

            var dto = new CreateAppointmentDto
            {
                PatientId = 1,
                AppointmentDate = existing.AppointmentDate,
                Reason = "Checkup"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAppointmentAsync(dto));
        }

        [Fact]
        public async Task CreateAppointmentAsync_CreatesAppointmentSuccessfully()
        {
            var (context, service) = CreateContextAndService();
            // Arrange - create patient first
            var patient = new Patient { Id = 1, FirstName = "Test", LastName = "Patient", Email = "test@test.com", IsDeleted = false };
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
            
            var dto = new CreateAppointmentDto
            {
                PatientId = 1,
                AppointmentDate = DateTime.Now.AddDays(1),
                Reason = "Checkup"
            };

            // Act
            var result = await service.CreateAppointmentAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.PatientId, result.PatientId);
            Assert.Equal(dto.Reason, result.Reason);
            Assert.Equal("Scheduled", result.Status);
        }

        [Fact]
        public async Task DeleteAppointmentAsync_CancelsAppointment()
        {
            var (context, service) = CreateContextAndService();
            // Arrange - create patient first
            var patient = new Patient { Id = 1, FirstName = "Test", LastName = "Patient", Email = "test@test.com", IsDeleted = false };
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
            
            var appt = new Appointment { Id = 1, PatientId = 1, Status = "Scheduled" };
            context.Appointments.Add(appt);
            await context.SaveChangesAsync();

            // Act
            var success = await service.DeleteAppointmentAsync(1);

            // Assert
            Assert.True(success);
            Assert.Equal("Cancelled", appt.Status);
        }

        [Fact]
        public async Task CreateAppointmentAsync_ThrowsArgumentException_WhenPatientMissing()
        {
            var (context, service) = CreateContextAndService();
            var dto = new CreateAppointmentDto
            {
                PatientId = 999,
                AppointmentDate = DateTime.Now.AddDays(1),
                Reason = "Test"
            };

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAppointmentAsync(dto));
        }

        [Fact]
        public async Task UpdateAppointmentAsync_ThrowsConflict_WhenNewDateConflicts()
        {
            var (context, service) = CreateContextAndService();
            // arrange patient and two appointments
            var patient = new Patient { Id = 1, FirstName = "T", LastName = "P", Email = "t@p.com", IsDeleted = false };
            context.Patients.Add(patient);
            var appt1 = new Appointment { Id = 1, PatientId = 1, AppointmentDate = new DateTime(2024,1,1,10,0,0), Status = "Scheduled" };
            var appt2 = new Appointment { Id = 2, PatientId = 1, AppointmentDate = new DateTime(2024,1,2,10,0,0), Status = "Scheduled" };
            context.Appointments.AddRange(appt1, appt2);
            await context.SaveChangesAsync();

            var updateDto = new UpdateAppointmentDto { AppointmentDate = appt1.AppointmentDate }; // conflict with appt1
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAppointmentAsync(2, updateDto));
        }

        [Fact]
        public async Task GetAppointmentsForDateAsync_ExcludesCancelled()
        {
            var (context, service) = CreateContextAndService();
            var patient = new Patient { Id = 1, FirstName = "T", LastName = "P", Email = "t@p.com", IsDeleted = false };
            context.Patients.Add(patient);
            var today = DateTime.Today;
            context.Appointments.Add(new Appointment { Id = 1, PatientId = 1, AppointmentDate = today, Status = "Cancelled" });
            context.Appointments.Add(new Appointment { Id = 2, PatientId = 1, AppointmentDate = today, Status = "Scheduled" });
            await context.SaveChangesAsync();

            var list = await service.GetAppointmentsForDateAsync(today);
            Assert.Single(list);
            Assert.Equal("Scheduled", list[0].Status);
        }
    }
}
