# Medical Customer Management System - AI Agent Instructions

This is a .NET C# web application for managing patient records, appointments, and medical history in a healthcare context.

## Project Architecture

### Core Components
- **Models**: Patient, Appointment, MedicalHistory entities with DTOs
- **Services**: PatientService, AppointmentService, MedicalHistoryService (business logic layer)
- **Controllers**: PatientsController, AppointmentsController, MedicalHistoryController (RESTful API)
- **Data**: Entity Framework Core with SQL Server LocalDB, soft deletes for HIPAA compliance

### Key Directories
- `Models/` - Domain entities and DTOs (Patient, Appointment, MedicalHistory)
- `Services/` - Business logic with interfaces for dependency injection
- `Controllers/` - RESTful API endpoints with error handling
- `Data/` - MedicalDbContext with relationship configuration and indexes
- `.vscode/` - Build tasks (build, run, watch, test, database-update)

## Development Workflows

### Build & Run
```powershell
dotnet build
dotnet run
```

### Watch Mode (auto-reload)
```powershell
dotnet watch run
```

### Database
```powershell
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Tests
```powershell
dotnet test
```

## Architecture Patterns

### Service-DTO Pattern
- All controllers use services through dependency injection
- Services return DTOs, not entities (never expose PHI directly)
- Example: `PatientService.GetPatientByIdAsync()` returns `PatientDto` (excludes insurance/address)

### Data Access Patterns
- Soft deletes: Check `IsDeleted` flag in queries (`WHERE !p.IsDeleted`)
- Indexes on frequently queried fields: Patient.Email, Appointment dates, MedicalHistory dates
- Always filter by patient in appointment/history queries for isolation

### Appointment Conflict Detection
- `AppointmentService.CheckConflictAsync()` prevents double-booking
- Called automatically before creation/update
- Throws `InvalidOperationException` if conflict exists

## Project-Specific Patterns

### PHI Protection (Patient Health Information)
- Never log patient names, emails, or medical details
- Use DTOs to exclude sensitive fields (insurance number, full address)
- Examples in `Models/DTOs/PatientDto.cs` - excludes `MedicalInsuranceNumber`

### Medical History - Immutable Records
- Each record includes VisitDate, Diagnosis, Treatment, Symptoms, Medications
- Created via `MedicalHistoryService.CreateHistoryAsync()` only (no updates)
- Soft delete for compliance: `IsDeleted` flag instead of hard delete

### Status Tracking
- Appointments: "Scheduled", "In Progress", "Completed", "Cancelled"
- Deletion = status change to "Cancelled", not removal
- Patients: `IsActive` and `IsDeleted` flags for soft deletion

## Configuration

- **Connection String**: `appsettings.json` → `ConnectionStrings:MedicalDb`
- **JWT**: `Jwt:Key` (change in production), `ExpirationMinutes: 60`
- **CORS**: Configured for localhost:3000 and localhost:4200 (medical facility domains)
- **Database**: LocalDB by default; modify connection string for SQL Server

## Common Tasks

**Add new medical entity**: Create model → Create DbSet + configure in MedicalDbContext → Create service interface/implementation → Add controller → Create migration

**Query patient records**: Always use service methods with date filters:
```csharp
// Get recent appointments (better than unfiltered)
await appointmentService.GetPatientAppointmentsAsync(patientId)

// Get history with date range
await historyService.GetPatientHistoryAsync(patientId, startDate, endDate)
```

**Update patient**: Use `PatientService.UpdatePatientAsync()` only (logs UpdatedAt timestamp for audit)

**Create appointment**: `AppointmentService.CreateAppointmentAsync()` validates conflicts automatically

## External Dependencies

- Entity Framework Core 8.0+
- Swashbuckle (Swagger UI at `/swagger`)
- Microsoft.AspNetCore.Identity (user authentication)
