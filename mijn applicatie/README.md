# Medical Customer Management System

A modern .NET C# web application for managing patient records, appointments, and medical history in a healthcare context.

## Features

- **Patient Management**: Create, read, update, and manage patient profiles with sensitive health information (PHI) protection
- **Appointment Scheduling**: Schedule and track patient appointments with automatic conflict detection
- **Medical History**: Maintain immutable medical records with timestamps and audit trails
- **Role-Based Access Control**: Doctor, Patient, and Admin roles with JWT authentication
- **HIPAA Compliance**: Soft deletes for compliance, secure data handling, audit logging
- **RESTful API**: Complete API documentation with Swagger/OpenAPI

## Architecture

```
MedicalCustomerManagement/
├── Models/              # Domain entities and DTOs
│   ├── Patient.cs
│   ├── Appointment.cs
│   ├── MedicalHistory.cs
│   └── DTOs/
├── Services/            # Business logic layer
│   ├── PatientService.cs
│   ├── AppointmentService.cs
│   └── MedicalHistoryService.cs
├── Controllers/         # API endpoints
│   ├── PatientsController.cs
│   ├── AppointmentsController.cs
│   └── MedicalHistoryController.cs
├── Data/                # Database context and migrations
│   └── MedicalDbContext.cs
└── Program.cs           # Startup configuration
```

## Prerequisites

- .NET 8.0 SDK or later
- SQL Server (LocalDB) or SQL Server Express
- Visual Studio 2022 or Visual Studio Code

## Getting Started

### 1. Build the Project

```powershell
dotnet build
```

### 2. Configure Database

Edit `appsettings.json` to update your connection string:

```json
"ConnectionStrings": {
  "MedicalDb": "Server=YOUR_SERVER;Database=MedicalCustomerManagement;Trusted_Connection=true;"
}
```

### 3. Apply Database Migrations

```powershell
dotnet ef database update
```

### 4. Run the Application

```powershell
dotnet run
```

The API will be available at `https://localhost:7000` (HTTPS) or `http://localhost:5000` (HTTP).

### 5. Access Swagger Documentation

Visit `https://localhost:7000/swagger` to view and test API endpoints interactively.

## API Endpoints

### Patients
- `GET /api/patients` - Get all active patients
- `GET /api/patients/{id}` - Get patient by ID
- `POST /api/patients` - Create new patient
- `PUT /api/patients/{id}` - Update patient information
- `DELETE /api/patients/{id}` - Soft delete patient

### Appointments
- `GET /api/appointments/{id}` - Get appointment by ID
- `GET /api/appointments/patient/{patientId}` - Get patient's appointments
- `GET /api/appointments/date/{date}` - Get appointments for a specific date
- `POST /api/appointments` - Create new appointment
- `PUT /api/appointments/{id}` - Update appointment
- `DELETE /api/appointments/{id}` - Cancel appointment

### Medical History
- `GET /api/medicalhistory/{id}` - Get medical history record
- `GET /api/medicalhistory/patient/{patientId}` - Get patient's medical history
- `POST /api/medicalhistory` - Create medical history record

## Development

### Running Tests

```powershell
dotnet test
```

### Creating Database Migrations

```powershell
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Database Structure

#### Patients Table
- Patient demographic and contact information
- Medical insurance details
- Soft delete support (IsDeleted flag)
- Unique email constraint

#### Appointments Table
- Patient and doctor references
- Appointment scheduling with date/time
- Status tracking (Scheduled, In Progress, Completed, Cancelled)
- Conflict detection to prevent double-booking

#### Medical History Table
- Immutable medical records
- Doctor diagnosis and treatment information
- Symptoms and medication tracking
- Soft delete support for compliance

## Security Considerations

- **Authentication**: JWT tokens (configure in `appsettings.json`)
- **Authorization**: Role-based access control (Doctor, Patient, Admin)
- **Data Protection**: All patient data treated as PHI (Protected Health Information)
- **HTTPS**: Enforced in production
- **CORS**: Configured for medical facility domains only

## Configuration

Key settings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MedicalDb": "..."  // Database connection string
  },
  "Jwt": {
    "Key": "your-secret-key",
    "Issuer": "MedicalApp",
    "Audience": "MedicalAppUsers",
    "ExpirationMinutes": 60
  }
}
```

## Project-Specific Patterns

### Patient Data Handling
- Never log patient names or medical details to console
- Use DTOs to exclude sensitive fields from API responses
- Always check `IsDeleted` and `IsActive` flags in queries

### Medical History Pattern
- Store immutable records with timestamps
- Use soft deletes instead of hard deletes for compliance
- Always include date filters for performance optimization

### Appointment Management
- Always check for conflicts before creating appointments
- Use service methods for booking to ensure validation
- Implement cancellation instead of deletion

## Common Tasks

### Add a New Medical Service

1. Create model in `Models/`
2. Add `DbSet<Model>` to `MedicalDbContext`
3. Create service interface and implementation in `Services/`
4. Register service in `Program.cs`
5. Add controller in `Controllers/`
6. Create migration: `dotnet ef migrations add ModelName`

### Query Patient Records

Always include date filters for better performance:

```csharp
// Example: Get patient's recent appointments
var appointments = await _appointmentService.GetPatientAppointmentsAsync(patientId);

// Example: Get medical history with date range
var history = await _historyService.GetPatientHistoryAsync(
    patientId, 
    startDate: DateTime.Now.AddMonths(-6), 
    endDate: DateTime.Now
);
```

### Update Patient Information

Only use the dedicated service method to trigger audit logging:

```csharp
var updatedPatient = await _patientService.UpdatePatientAsync(id, updateDto);
```

## Troubleshooting

### Database Connection Issues
- Verify SQL Server LocalDB is running
- Check connection string in `appsettings.json`
- Run `dotnet ef database update` to ensure migrations are applied

### Migration Issues
- Delete migrations and start fresh if needed
- Ensure database exists before applying migrations
- Check for conflicting entity configurations

### Authentication Issues
- Verify JWT key is configured in `appsettings.json`
- Ensure Identity framework is properly initialized
- Check token expiration settings

## License

This project is part of the Medical Customer Management System.
