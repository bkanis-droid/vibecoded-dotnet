# Medical Customer Management – Development Repository

This repository contains a .NET 8 web application for managing patient records, appointments, and medical history in a healthcare context, along with the tools needed to run it in a Windows Sandbox environment.

## Repository Structure

```
sandbox_share/
├── mijn applicatie/        # Main .NET 8 web application
├── vibecode.wsb            # Windows Sandbox configuration file
├── dotnet-sdk-8.0.418-win-x64.exe   # .NET 8 SDK installer (for sandbox use)
└── VSCodeUserSetup-x64-1.108.1.exe  # VS Code installer (for sandbox use)
```

## Application

The `mijn applicatie/` folder contains the **Medical Customer Management System** – a RESTful ASP.NET Core 8 API with a frontend served as static files.

Key features:
- Patient management (CRUD with soft deletes)
- Appointment scheduling with conflict detection
- Immutable medical history records with audit trails
- Role-based access control (Doctor, Patient, Admin) via ASP.NET Identity
- SQLite database (via Entity Framework Core)
- Swagger/OpenAPI documentation

See [mijn applicatie/README.md](mijn%20applicatie/README.md) for full setup and usage instructions.

## Windows Sandbox Setup

The `vibecode.wsb` file configures a Windows Sandbox that mounts this folder, so you can develop in a clean, isolated environment without affecting your host machine.

**To start the sandbox:**
1. Double-click `vibecode.wsb` (requires Windows 10/11 Pro or Enterprise with Sandbox enabled).
2. Inside the sandbox, run the `.exe` installers to set up .NET 8 SDK and VS Code.
3. Open the `mijn applicatie/` folder in VS Code and start developing.

## Quick Start (outside sandbox)

```powershell
cd "mijn applicatie"
dotnet restore
dotnet run
```

The API will be available at `https://localhost:5001` and Swagger UI at `https://localhost:5001/swagger`.

## Prerequisites

- Windows 10/11 (Pro/Enterprise for Sandbox support)
- .NET 8.0 SDK
- Visual Studio 2022 or Visual Studio Code
