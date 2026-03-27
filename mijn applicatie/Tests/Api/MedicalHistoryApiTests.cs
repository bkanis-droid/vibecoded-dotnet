using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using MedicalCustomerManagement.Models.DTOs;
using MedicalCustomerManagement.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.IO;
using System.Linq;
using MedicalCustomerManagement.Services;
using Moq;

namespace MedicalCustomerManagement.Tests.Api
{
    public class MedicalHistoryApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;

        public MedicalHistoryApiTests(WebApplicationFactory<Program> factory)
        {
            _connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseContentRoot(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..\\..\\..")));
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MedicalDbContext>));
                    if (descriptor != null) services.Remove(descriptor);
                    services.AddDbContext<MedicalDbContext>(opts => opts.UseSqlite(_connection));
                    
                    // Mock IAuditService to prevent dependency errors
                    var auditDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAuditService));
                    if (auditDescriptor != null) services.Remove(auditDescriptor);
                    var mockAudit = new Mock<IAuditService>();
                    mockAudit.Setup(a => a.LogActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
                    services.AddScoped(sp => mockAudit.Object);
                    
                    // ensure schema is created
                    using (var scope = services.BuildServiceProvider().CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<MedicalDbContext>();
                        dbContext.Database.EnsureCreated();
                    }
                });
            });
            _client = _factory.CreateClient();
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _client?.Dispose();
            _factory?.Dispose();
        }

        private async Task<int> CreateTestPatient()
        {
            var patientDto = new CreatePatientDto
            {
                FirstName = "Test",
                LastName = "Patient",
                Email = "test@example.com",
                PhoneNumber = "+1234567890",
                DateOfBirth = new DateTime(1990, 1, 1)
            };
            var response = await _client.PostAsJsonAsync("/api/patients", patientDto);
            response.EnsureSuccessStatusCode(); // Throw if not successful
            var result = await response.Content.ReadFromJsonAsync<PatientDto>();
            return result?.Id ?? throw new Exception("Patient creation failed");
        }

        [Fact]
        public async Task GetPatientHistory_ReturnsOkResult()
        {
            // Arrange - Create patient first
            var patientId = await CreateTestPatient();

            // Act
            var response = await _client.GetAsync($"/api/medicalhistory/patient/{patientId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateMedicalHistory_ReturnsCreatedResult()
        {
            // Arrange - Create patient first
            var patientId = await CreateTestPatient();

            var createDto = new CreateMedicalHistoryDto
            {
                PatientId = patientId,
                VisitDate = DateTime.Today.AddDays(1),
                Diagnosis = "Test diagnosis",
                Treatment = "Test treatment",
                Symptoms = "Test symptoms",
                Medications = "Test medications"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/medicalhistory", createDto);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<MedicalHistoryDto>();
            Assert.NotNull(result);
            Assert.Equal(patientId, result.PatientId);
            Assert.Equal("Test diagnosis", result.Diagnosis);
        }

        [Fact]
        public async Task CreateMedicalHistory_ReturnsBadRequest_WhenPatientDoesNotExist()
        {
            // Arrange
            var createDto = new CreateMedicalHistoryDto
            {
                PatientId = 999, // Non-existent patient
                VisitDate = DateTime.Now,
                Diagnosis = "Test diagnosis",
                Treatment = "Test treatment",
                Symptoms = "Test symptoms",
                Medications = "Test medications"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/medicalhistory", createDto);

            // Assert
            if (response.StatusCode != HttpStatusCode.BadRequest)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Expected BadRequest but got {response.StatusCode}: {body}");
            }
        }

        [Fact]
        public async Task CreateMedicalHistory_ReturnsBadRequest_WhenDiagnosisIsMissing()
        {
            // Arrange - ensure a valid patient exists so we don't hit FK errors
            var patientId = await CreateTestPatient();

            var createDto = new CreateMedicalHistoryDto
            {
                PatientId = patientId,
                VisitDate = DateTime.Now,
                Diagnosis = "", // empty string triggers validation
                Treatment = "Test treatment",
                Symptoms = "Test symptoms",
                Medications = "Test medications"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/medicalhistory", createDto);

            // Assert
            if (response.StatusCode != HttpStatusCode.BadRequest)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Expected BadRequest but got {response.StatusCode}: {body}");
            }
        }

        [Fact]
        public async Task GetHistoryById_ReturnsCreatedRecord()
        {
            var patientId = await CreateTestPatient();
            var createDto = new CreateMedicalHistoryDto
            {
                PatientId = patientId,
                VisitDate = DateTime.Today.AddDays(1),
                Diagnosis = "Diag",
            };
            var postResp = await _client.PostAsJsonAsync("/api/medicalhistory", createDto);
            postResp.EnsureSuccessStatusCode();
            var created = await postResp.Content.ReadFromJsonAsync<MedicalHistoryDto>();

            var getResp = await _client.GetAsync($"/api/medicalhistory/{created.Id}");
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
            var fetched = await getResp.Content.ReadFromJsonAsync<MedicalHistoryDto>();
            Assert.Equal(created.Id, fetched.Id);
        }

        [Fact]
        public async Task GetPatientHistory_ReturnsFilteredList()
        {
            var patientId = await CreateTestPatient();
            var dto1 = new CreateMedicalHistoryDto { PatientId = patientId, VisitDate = new DateTime(2024,1,1), Diagnosis = "A" };
            var dto2 = new CreateMedicalHistoryDto { PatientId = patientId, VisitDate = new DateTime(2024,6,1), Diagnosis = "B" };
            await _client.PostAsJsonAsync("/api/medicalhistory", dto1);
            await _client.PostAsJsonAsync("/api/medicalhistory", dto2);

            var response = await _client.GetAsync($"/api/medicalhistory/patient/{patientId}?startDate=2024-05-01&endDate=2024-12-31");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var list = await response.Content.ReadFromJsonAsync<List<MedicalHistoryDto>>();
            Assert.Single(list);
            Assert.Equal("B", list[0].Diagnosis);
        }
    }
}