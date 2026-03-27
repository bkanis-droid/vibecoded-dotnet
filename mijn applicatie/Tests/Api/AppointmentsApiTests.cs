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
    public class AppointmentsApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;

        public AppointmentsApiTests(WebApplicationFactory<Program> factory)
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
        public async Task GetPatientAppointments_ReturnsOkResult()
        {
            // Arrange - Create patient first
            var patientId = await CreateTestPatient();

            // Act
            var response = await _client.GetAsync($"/api/appointments/patient/{patientId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateAppointment_ReturnsCreatedResult()
        {
            // Arrange - Create patient first
            var patientId = await CreateTestPatient();

            var createDto = new CreateAppointmentDto
            {
                PatientId = patientId,
                AppointmentDate = DateTime.UtcNow.AddDays(2),
                Reason = "Test appointment"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/appointments", createDto);

            // Assert
            if (response.StatusCode != HttpStatusCode.Created)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Expected Created but got {response.StatusCode}: {body}");
            }

            var result = await response.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.NotNull(result);
            Assert.Equal(patientId, result.PatientId);
        }

        [Fact]
        public async Task CreateAppointment_ReturnsBadRequest_WhenPatientDoesNotExist()
        {
            // Arrange
            var createDto = new CreateAppointmentDto
            {
                PatientId = 999, // Non-existent patient
                AppointmentDate = DateTime.Now.AddDays(1),
                Reason = "Test appointment"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/appointments", createDto);

            // Assert
            if (response.StatusCode != HttpStatusCode.BadRequest)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Expected BadRequest but got {response.StatusCode}: {body}");
            }
        }

        [Fact]
        public async Task CreateAppointment_ReturnsConflict_WhenAppointmentConflicts()
        {
            // Arrange - First create a patient
            var patientId = await CreateTestPatient();

            var fixedDate = new DateTime(2024, 12, 1, 10, 0, 0);

            var createDto1 = new CreateAppointmentDto
            {
                PatientId = patientId,
                AppointmentDate = fixedDate,
                Reason = "First appointment"
            };

            var firstResponse = await _client.PostAsJsonAsync("/api/appointments", createDto1);
            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode); // Ensure first appointment is created

            // Now try to create a conflicting appointment
            var createDto2 = new CreateAppointmentDto
            {
                PatientId = patientId,
                AppointmentDate = fixedDate, // Same time as first
                Reason = "Conflicting appointment"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/appointments", createDto2);

            // Assert
            if (response.StatusCode != HttpStatusCode.Conflict)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Expected Conflict but got {response.StatusCode}: {body}");
            }
        }

        [Fact]
        public async Task GetAppointmentById_ReturnsCreatedAppointment()
        {
            var patientId = await CreateTestPatient();
            var dto = new CreateAppointmentDto
            {
                PatientId = patientId,
                AppointmentDate = DateTime.UtcNow.AddDays(3),
                Reason = "Check"
            };
            var post = await _client.PostAsJsonAsync("/api/appointments", dto);
            post.EnsureSuccessStatusCode();
            var created = await post.Content.ReadFromJsonAsync<AppointmentDto>();

            var get = await _client.GetAsync($"/api/appointments/{created.Id}");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var fetched = await get.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.Equal(created.Id, fetched.Id);
        }

        [Fact]
        public async Task GetAppointmentsByDate_ReturnsList()
        {
            var patientId = await CreateTestPatient();
            var date = new DateTime(2025,1,1,9,0,0);
            var dto = new CreateAppointmentDto { PatientId = patientId, AppointmentDate = date, Reason = "R" };
            await _client.PostAsJsonAsync("/api/appointments", dto);

            var resp = await _client.GetAsync($"/api/appointments/date/{date:yyyy-MM-dd}");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var list = await resp.Content.ReadFromJsonAsync<List<AppointmentDto>>();
            Assert.Single(list);
        }

        [Fact]
        public async Task UpdateAppointment_ReturnsOk_AndPersists()
        {
            var patientId = await CreateTestPatient();
            var createDto = new CreateAppointmentDto { PatientId = patientId, AppointmentDate = DateTime.UtcNow.AddDays(4) };
            var postResp = await _client.PostAsJsonAsync("/api/appointments", createDto);
            postResp.EnsureSuccessStatusCode();
            var appt = await postResp.Content.ReadFromJsonAsync<AppointmentDto>();

            var updateDto = new UpdateAppointmentDto { Reason = "Updated" };
            var putResp = await _client.PutAsJsonAsync($"/api/appointments/{appt.Id}", updateDto);
            Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);
            var updated = await putResp.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.Equal("Updated", updated.Reason);
        }

        [Fact]
        public async Task DeleteAppointment_MarksCancelled_AndExcludesFromLists()
        {
            var patientId = await CreateTestPatient();
            var createDto = new CreateAppointmentDto { PatientId = patientId, AppointmentDate = DateTime.UtcNow.AddDays(5) };
            var postResp = await _client.PostAsJsonAsync("/api/appointments", createDto);
            postResp.EnsureSuccessStatusCode();
            var appt = await postResp.Content.ReadFromJsonAsync<AppointmentDto>();

            var del = await _client.DeleteAsync($"/api/appointments/{appt.Id}");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

            // the appointment should still be retrievable by id but with cancelled status
            var get = await _client.GetAsync($"/api/appointments/{appt.Id}");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var fetched = await get.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.NotNull(fetched);
            Assert.Equal("Cancelled", fetched.Status);

            // it should no longer appear in the patient-specific listing
            var listResp = await _client.GetAsync($"/api/appointments/patient/{patientId}");
            Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
            var list = await listResp.Content.ReadFromJsonAsync<List<AppointmentDto>>();
            Assert.DoesNotContain(list, a => a.Id == appt.Id);

            // and date query should also omit the cancelled appointment
            var dateResp = await _client.GetAsync($"/api/appointments/date/{appt.AppointmentDate:yyyy-MM-dd}");
            Assert.Equal(HttpStatusCode.OK, dateResp.StatusCode);
            var dateList = await dateResp.Content.ReadFromJsonAsync<List<AppointmentDto>>();
            Assert.DoesNotContain(dateList, a => a.Id == appt.Id);
        }
    }
}