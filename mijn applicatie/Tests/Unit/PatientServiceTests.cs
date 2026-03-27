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
    public class PatientServiceTests
    {
        // helper to create fresh context/service pair for each test
        private (MedicalDbContext context, PatientService service) CreateContextAndService()
        {
            var conn = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            conn.Open();
            var options = new DbContextOptionsBuilder<MedicalDbContext>()
                .UseSqlite(conn)
                .Options;
            var context = new MedicalDbContext(options);
            // ensure schema exists for in-memory database
            context.Database.EnsureCreated();
            var mockLogger = new Mock<ILogger<PatientService>>();
            var mockAuditService = new Mock<IAuditService>();
            var service = new PatientService(context, mockLogger.Object, mockAuditService.Object);
            return (context, service);
        }

        [Fact]
        public async Task GetPatientByIdAsync_ReturnsPatient_WhenPatientExists()
        {
            var (context, service) = CreateContextAndService();
            // Arrange
            var patient = new Patient
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                IsDeleted = false
            };
            context.Patients.Add(patient);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetPatientByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John", result.FirstName);
            Assert.Equal("Doe", result.LastName);
        }

        [Fact]
        public async Task GetPatientByIdAsync_ReturnsNull_WhenPatientNotFound()
        {
            var (context, service) = CreateContextAndService();
            // Act
            var result = await service.GetPatientByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetPatientByIdAsync_ReturnsNull_WhenPatientIsDeleted()
        {
            var (context, service) = CreateContextAndService();
            // Arrange
            var patient = new Patient { Id = 2, IsDeleted = true };
            context.Patients.Add(patient);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetPatientByIdAsync(2);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreatePatientAsync_CreatesPatientSuccessfully()
        {
            var (context, service) = CreateContextAndService();
            // Arrange
            var createDto = new CreatePatientDto
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                PhoneNumber = "1234567890",
                DateOfBirth = new DateTime(1990, 1, 1)
            };

            // Act
            var result = await service.CreatePatientAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Jane", result.FirstName);
            Assert.Equal("Smith", result.LastName);
            Assert.Equal("jane.smith@example.com", result.Email);
        }

        [Fact]
        public async Task UpdatePatientAsync_UpdatesPatientSuccessfully()
        {
            var (context, service) = CreateContextAndService();
            // Arrange
            var existingPatient = new Patient
            {
                Id = 3,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                IsDeleted = false
            };
            context.Patients.Add(existingPatient);
            await context.SaveChangesAsync();

            var updateDto = new UpdatePatientDto
            {
                FirstName = "Johnny",
                PhoneNumber = "0987654321"
            };

            // Act
            var result = await service.UpdatePatientAsync(3, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Johnny", result.FirstName);
            Assert.Equal("0987654321", result.PhoneNumber);
        }

        [Fact]
        public async Task DeletePatientAsync_SoftDeletesPatient()
        {
            var (context, service) = CreateContextAndService();
            // Arrange
            var patient = new Patient { Id = 4, IsDeleted = false };
            context.Patients.Add(patient);
            await context.SaveChangesAsync();

            // Act
            var result = await service.DeletePatientAsync(4);

            // Assert
            Assert.True(result);
            Assert.True(patient.IsDeleted);
        }
    }
}
