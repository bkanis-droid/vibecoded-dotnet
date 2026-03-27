using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Net.Http.Json;
using MedicalCustomerManagement.Models.DTOs;
using MedicalCustomerManagement.Data;
using System.Net;
using System.IO;
using System.Linq;
using MedicalCustomerManagement.Services;
using Moq;

namespace MedicalCustomerManagement.Tests.Api
{
    public class PatientsApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;

        public PatientsApiTests(WebApplicationFactory<Program> factory)
        {
            // open a shared in-memory SQLite connection so that the database persists
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

        [Fact]
        public async Task GetAllPatients_ReturnsOkResult()
        {
            // Act
            var response = await _client.GetAsync("/api/patients");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreatePatient_ReturnsCreatedResult()
        {
            // Arrange
            var createDto = new CreatePatientDto
            {
                FirstName = "Test",
                LastName = "Patient",
                Email = "test.patient@example.com",
                PhoneNumber = "1234567890",
                DateOfBirth = new DateTime(1990, 1, 1)
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/patients", createDto);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<PatientDto>();
            Assert.NotNull(result);
            Assert.Equal("Test", result.FirstName);
            Assert.Equal("Patient", result.LastName);
        }

        [Fact]
        public async Task GetPatientById_ReturnsNotFound_WhenPatientDoesNotExist()
        {
            // Act
            var response = await _client.GetAsync("/api/patients/999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetPatientById_ReturnsOk_WhenPatientExists()
        {
            // arrange
            var createDto = new CreatePatientDto
            {
                FirstName = "Foo",
                LastName = "Bar",
                Email = "foo.bar@example.com",
                PhoneNumber = "123",
                DateOfBirth = new DateTime(1980, 1, 1)
            };
            var createResp = await _client.PostAsJsonAsync("/api/patients", createDto);
            createResp.EnsureSuccessStatusCode();
            var created = await createResp.Content.ReadFromJsonAsync<PatientDto>();

            // act
            var response = await _client.GetAsync($"/api/patients/{created.Id}");

            // assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var fetched = await response.Content.ReadFromJsonAsync<PatientDto>();
            Assert.Equal(created.Id, fetched.Id);
        }

        [Fact]
        public async Task CreatePatient_ReturnsBadRequest_WhenEmailIsMissing()
        {
            // Arrange
            var createDto = new CreatePatientDto
            {
                FirstName = "Test",
                LastName = "Patient",
                PhoneNumber = "1234567890",
                DateOfBirth = new DateTime(1990, 1, 1)
                // Email is missing
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/patients", createDto);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdatePatient_ReturnsOk_AndPersistsChanges()
        {
            var createDto = new CreatePatientDto
            {
                FirstName = "Orig",
                LastName = "Name",
                Email = "orig@example.com",
                PhoneNumber = "111",
                DateOfBirth = new DateTime(1990, 1, 1)
            };
            var createResp = await _client.PostAsJsonAsync("/api/patients", createDto);
            createResp.EnsureSuccessStatusCode();
            var patient = await createResp.Content.ReadFromJsonAsync<PatientDto>();

            var updateDto = new UpdatePatientDto
            {
                FirstName = "Updated",
                PhoneNumber = "222"
            };

            var updateResp = await _client.PutAsJsonAsync($"/api/patients/{patient.Id}", updateDto);
            Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

            var fetched = await updateResp.Content.ReadFromJsonAsync<PatientDto>();
            Assert.Equal("Updated", fetched.FirstName);
            Assert.Equal("222", fetched.PhoneNumber);
        }

        [Fact]
        public async Task DeletePatient_ReturnsNoContent_AndThenNotFound()
        {
            var createDto = new CreatePatientDto
            {
                FirstName = "ToDelete",
                LastName = "User",
                Email = "del@example.com",
                PhoneNumber = "333",
                DateOfBirth = new DateTime(1990, 1, 1)
            };
            var createResp = await _client.PostAsJsonAsync("/api/patients", createDto);
            createResp.EnsureSuccessStatusCode();
            var patient = await createResp.Content.ReadFromJsonAsync<PatientDto>();

            var delResp = await _client.DeleteAsync($"/api/patients/{patient.Id}");
            Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

            var getResp = await _client.GetAsync($"/api/patients/{patient.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
        }
    }
}