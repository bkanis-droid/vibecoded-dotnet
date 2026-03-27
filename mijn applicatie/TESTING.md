# Running Tests

This project contains unit tests, API integration tests, and UI tests using xUnit and Playwright.

## Quick Start

### Option 1: Automated Setup (Recommended)

Use the provided setup scripts to automatically install dependencies and run tests:

**Windows (PowerShell):**
```powershell
.\setup-tests.ps1
```

**Windows (Batch):**
```batch
setup-tests.bat
```

### Option 2: Manual Setup

1. **Install Playwright browsers** (one-time setup):
   ```
   playwright install chromium
   ```

2. **Build the project:**
   ```
   dotnet build "Mijn applicatie.sln"
   ```

3. **Run tests:**
   ```
   dotnet test "Mijn applicatie.sln"
   ```

## Test Categories

- **Unit Tests** (Tests/Unit/): Service logic tests with mocked dependencies
- **API Integration Tests** (Tests/Api/): RESTful API endpoint tests
- **UI Tests** (Tests/Ui/): Browser-based end-to-end tests (currently skipped - require manual setup)

## Test Results

### Expected Output:
- **✓ 39 tests pass** (Unit + API tests)
- **⏭️ 14 tests skipped** (UI tests - require server/Playwright configuration)

### Running Specific Tests:

Only non-UI tests:
```
dotnet test --filter "Category!=UI"
```

Only unit tests:
```
dotnet test Tests/Unit/
```

Only API tests:
```
dotnet test Tests/Api/
```

## What's in the Tests?

### Unit Tests
- Patient service CRUD operations
- Appointment conflict detection
- Medical history creation and retrieval
- Soft delete operations

### API Tests
- Patient endpoints (GET, POST, PUT, DELETE)
- Appointment endpoints with conflict checking
- Medical history endpoints
- Error handling for missing/invalid data

### UI Tests (Skipped)
- Page navigation and form interaction
- Patient/Appointment/Medical history CRUD via UI
- Form validation
- Requires: Running server + Playwright browser configuration

## Troubleshooting

**Playwright browser errors:**
```
playwright install chromium
```

**Build errors:**
```
dotnet clean
dotnet build "Mijn applicatie.sln"
```

**Port already in use (5000):**
Edit test fixture to use different port or stop the running application.

## CI/CD Integration

For automated pipelines, run:
```
dotnet test --verbosity minimal --logger "console;verbosity=minimal"
```

This runs only the stable unit and API tests (skips UI tests).
