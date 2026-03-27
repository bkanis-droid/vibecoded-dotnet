using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MedicalCustomerManagement.Data;
using MedicalCustomerManagement.Services;
using MedicalCustomerManagement.Middlewares;

var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Entity Framework Core with SQLite
        builder.Services.AddDbContext<MedicalDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("MedicalDb") ??
                "Data Source=medical.db"));

        // Identity
        builder.Services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<MedicalDbContext>()
            .AddDefaultTokenProviders();

        // JWT Authentication
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();

        // Custom Services
        builder.Services.AddScoped<IPatientService, PatientService>();
        builder.Services.AddScoped<IAppointmentService, AppointmentService>();
        builder.Services.AddScoped<IMedicalHistoryService, MedicalHistoryService>();
        builder.Services.AddScoped<IAuditService, AuditService>();

        // CORS for medical facilities
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("MedicalFacilities", policy =>
            {
                policy.WithOrigins("http://localhost:3000", "http://localhost:4200")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline
        // global error handler
        app.UseMiddleware<ExceptionMiddleware>();

        app.UseSwagger();
        app.UseSwaggerUI();

        // Serve static files (HTML, CSS, JS)
        app.UseStaticFiles();

        // Disable HTTPS redirection for local development
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseCors("MedicalFacilities");
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Health check endpoint for server startup verification
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        // Serve index.html for root path
        app.MapGet("/", async (HttpContext context) =>
        {
            var filePath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "index.html");
            if (File.Exists(filePath))
            {
                context.Response.ContentType = "text/html";
                await context.Response.SendFileAsync(filePath);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsJsonAsync(new { message = $"File not found: {filePath}" });
            }
        });

        // Ensure database and schema are created on startup
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MedicalDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            try
            {
                logger.LogInformation("Initializing database...");
                
                // Always ensure schema exists (like tests do)
                dbContext.Database.EnsureCreated();
                logger.LogInformation("Database schema created/verified successfully");
                
                // Then try to apply any pending migrations if they exist
                var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying {MigrationCount} pending migrations...", pendingMigrations.Count);
                    dbContext.Database.Migrate();
                    logger.LogInformation("Migrations applied successfully");
                }
                else
                {
                    logger.LogInformation("No pending migrations to apply");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during database initialization");
                throw;
            }
        }

        app.Run();
