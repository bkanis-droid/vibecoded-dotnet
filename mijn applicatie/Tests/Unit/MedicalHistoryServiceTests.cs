using Xunit;
using MedicalCustomerManagement.Services;
using MedicalCustomerManagement.Models;
using MedicalCustomerManagement.Models.DTOs;
using MedicalCustomerManagement.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;

namespace MedicalCustomerManagement.Tests.Unit
{
    public class MedicalHistoryServiceTests
    {
        private (MedicalDbContext context, MedicalHistoryService service) CreateContextAndService()
        {
            var conn = new SqliteConnection("DataSource=:memory:");
            conn.Open();
            var options = new DbContextOptionsBuilder<MedicalDbContext>()
                .UseSqlite(conn)
                .Options;
            var context = new MedicalDbContext(options);
            context.Database.EnsureCreated();
            var mockLogger = new Mock<ILogger<MedicalHistoryService>>();
            var mockAudit = new Mock<IAuditService>();
            var service = new MedicalHistoryService(context, mockLogger.Object, mockAudit.Object);
            return (context, service);
        }

        [Fact]
        public async Task CreateHistoryAsync_CreatesSuccessfully_WhenPatientExists()
        {
            var (context, service) = CreateContextAndService();
            // arrange patient
            context.Patients.Add(new Patient { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", IsDeleted = false });
            await context.SaveChangesAsync();

            var dto = new CreateMedicalHistoryDto
            {
                PatientId = 1,
                VisitDate = DateTime.Today,
                Diagnosis = "diag",
                Treatment = "treat"
            };

            var result = await service.CreateHistoryAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(1, result.PatientId);
            Assert.Equal("diag", result.Diagnosis);
        }

        [Fact]
        public async Task CreateHistoryAsync_ThrowsArgumentException_WhenPatientMissing()
        {
            var (context, service) = CreateContextAndService();
            var dto = new CreateMedicalHistoryDto
            {
                PatientId = 999,
                VisitDate = DateTime.Today,
                Diagnosis = "diag"
            };

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateHistoryAsync(dto));
        }

        [Fact]
        public async Task GetHistoryByIdAsync_ReturnsNull_WhenNotFound()
        {
            var (context, service) = CreateContextAndService();
            var res = await service.GetHistoryByIdAsync(42);
            Assert.Null(res);
        }

        [Fact]
        public async Task GetPatientHistoryAsync_FiltersByDateRange()
        {
            var (context, service) = CreateContextAndService();
            context.Patients.Add(new Patient { Id = 1, FirstName = "X", LastName = "Y", Email = "x@y.com", IsDeleted = false });
            context.MedicalHistories.Add(new MedicalHistory { PatientId = 1, VisitDate = new DateTime(2024,1,1), Diagnosis = "d1" });
            context.MedicalHistories.Add(new MedicalHistory { PatientId = 1, VisitDate = new DateTime(2025,1,1), Diagnosis = "d2" });
            await context.SaveChangesAsync();

            var list = await service.GetPatientHistoryAsync(1, startDate: new DateTime(2024,6,1), endDate: new DateTime(2025,6,1));
            Assert.Single(list);
            Assert.Equal("d2", list[0].Diagnosis);
        }
    }
}
