using Microsoft.Playwright;
using Xunit;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Moq;
using MedicalCustomerManagement.Services;
using MedicalCustomerManagement.Data;

namespace MedicalCustomerManagement.Tests.Ui
{
    [Collection("Server Collection")]
    [Trait("Category", "UI")]
    public class PatientManagementUiTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly WebApplicationFactory<Program> _factory;
        private IBrowser? _browser;
        private IPage? _page;
        private int _testPatientId;
        private string _baseUrl;

        public PatientManagementUiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseUrls("http://localhost:5000");
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<MedicalDbContext>));
                    if (descriptor != null) services.Remove(descriptor);
                    services.AddDbContext<MedicalDbContext>(opts => opts.UseSqlite(new SqliteConnection("DataSource=:memory:")));
                    
                    var auditDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAuditService));
                    if (auditDescriptor != null) services.Remove(auditDescriptor);
                    var mockAudit = new Mock<IAuditService>();
                    mockAudit.Setup(a => a.LogActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
                    services.AddScoped(sp => mockAudit.Object);
                    
                    using (var scope = services.BuildServiceProvider().CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<MedicalDbContext>();
                        dbContext.Database.EnsureCreated();
                    }
                });
            });
            _baseUrl = "http://localhost:5000";
        }

        private async Task CreateTestPatientAsync()
        {
            using var client = new System.Net.Http.HttpClient();
            var dto = new
            {
                FirstName = "UI",
                LastName = "Patient",
                Email = "ui.patient@example.com",
                PhoneNumber = "0000000000",
                DateOfBirth = "1990-01-01"
            };
            var resp = await client.PostAsJsonAsync($"{_baseUrl}/api/patients", dto);
            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                if (body.TryGetProperty("id", out var idElem) && idElem.TryGetInt32(out var id))
                    _testPatientId = id;
            }
        }

        public async Task InitializeAsync()
        {
            // Skip initialization for UI tests - they require manual setup
            // Just set defaults to prevent null reference errors
            _baseUrl = "http://localhost:5000";
            _testPatientId = 0;
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (_browser != null)
            {
                await _browser.CloseAsync();
            }
        }

        [Fact]
        public async Task PatientListPage_LoadsSuccessfully()
        {
            // Arrange & Act
            await _page.GotoAsync(_baseUrl);

            // Assert
            var heading = await _page.Locator("h1").InnerTextAsync();
            Assert.Contains("Patient Management", heading);
        }

        [Fact]
        public async Task CanNavigateToAddPatientForm()
        {
            // Arrange
            await _page.GotoAsync(_baseUrl);

            // Act
            await _page.ClickAsync("text=Add New Patient");

            // Assert
            var subheading = await _page.Locator("h2").InnerTextAsync();
            Assert.Contains("Add New Patient", subheading);
        }

        [Fact]
        public async Task CanSubmitPatientForm()
        {
            // Arrange
            await _page.GotoAsync(_baseUrl);
            await _page.ClickAsync("text=Add New Patient");

            // Act
            await _page.FillAsync("#firstName", "UI Test");
            await _page.FillAsync("#lastName", "Patient");
            await _page.FillAsync("#email", "ui.test@example.com");
            await _page.FillAsync("#phoneNumber", "0987654321");
            await _page.FillAsync("#dateOfBirth", "1985-05-15");
            await _page.ClickAsync("button[type='submit']");

            // Assert - Should redirect back to patient list
            var heading = await _page.Locator("h1").InnerTextAsync();
            Assert.Contains("Patient Management", heading);

            // verify the new patient shows up in the table
            var rowText = await _page.Locator("table tr").First.InnerTextAsync();
            Assert.Contains("UI Test", rowText); // first row should be our new entry
        }

        [Fact]
        public async Task PatientFormValidation_Works()
        {
            // Arrange
            await _page.GotoAsync(_baseUrl);
            await _page.ClickAsync("text=Add New Patient");

            // Act - Try to submit empty form
            await _page.ClickAsync("button[type='submit']");

            // Assert - Should show validation errors
            var visible = await _page.Locator(".validation-error").IsVisibleAsync();
            Assert.True(visible);
        }
    }
}