# Session Notes: Database Persistence Implementation
**Date:** 2026-01-12

## What Was Done This Session

### 1. Fixed App Mapping CSV Import
- Changed duplicate detection logic so only **repository names** must be unique (not ServiceNow app names)
- Multiple repos can now map to the same ServiceNow application
- Location: `/Components/Pages/AppNameMappingImport.razor`

### 2. Added Capability Mapping Feature
- New tabbed interface with "Repository Mapping" and "Capability Mapping" tabs
- 1:1 mapping between ServiceNow application names and capabilities
- Added `CapabilityMapping` record and service methods to `IMockDataService`

### 3. Docker SQL Server Setup
- Created `docker-compose.yml` with Azure SQL Edge (ARM compatible for Mac M1/M2)
- Container: `lifecycle-sql`
- Port: `1433`
- Credentials: `sa` / `LifecycleDev123!`
- Database: `LifecycleDashboard`

### 4. Entity Framework Core Integration
Full database persistence layer with:
- DbContext and entity models
- Entity-to-model mappers
- Initial migration (applied)
- Database seeder

---

## Key Files Created/Modified

### Database Layer (`/Data/`)
| File | Purpose |
|------|---------|
| `LifecycleDbContext.cs` | EF Core DbContext with all DbSets and table configurations |
| `DesignTimeDbContextFactory.cs` | Enables EF Core CLI tools for migrations |
| `DatabaseSeeder.cs` | Seeds database with mock data |
| `EntityMappers.cs` | Bidirectional mapping between entities and domain models |
| `Entities/ApplicationEntity.cs` | Application table entity |
| `Entities/LifecycleTaskEntity.cs` | Task table entity |
| `Entities/UserEntity.cs` | User table entity |
| `Entities/RepositoryInfoEntity.cs` | Repository table entity (nested objects as JSON) |
| `Entities/AppNameMappingEntity.cs` | Contains multiple entities (mappings, audit, sync, etc.) |

### Configuration
| File | Purpose |
|------|---------|
| `docker-compose.yml` | Docker config for SQL Server |
| `appsettings.Development.json` | Connection string + `UseDatabase` toggle |
| `Program.cs` | DbContext registration + auto-seeding |

### Migrations
| File | Purpose |
|------|---------|
| `Migrations/20260112200859_InitialCreate.cs` | Initial database schema |

---

## How to Use the Database

### Prerequisites
1. Docker Desktop installed and running
2. .NET 10 SDK

### Start Database
```bash
cd /Users/benjaminhoffman/Documents/code/lifecycle/src/LifecycleDashboard
docker-compose up -d
```

### Enable Database Mode
Edit `appsettings.Development.json`:
```json
{
  "UseDatabase": true
}
```

### Run the App
```bash
dotnet run
```
On first run with `UseDatabase: true`, the database will auto-seed with mock data.

### Stop Database
```bash
docker-compose down
```

### Database Management Commands
```bash
# Create new migration
dotnet ef migrations add <MigrationName>

# Apply migrations
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script
```

---

## Current State

### What Works
- ✅ Docker SQL Server container runs
- ✅ Database created with all tables
- ✅ Initial migration applied
- ✅ Database seeding works
- ✅ App runs in mock mode (default)
- ✅ App runs in database mode when enabled

### What's NOT Implemented Yet
- ❌ Services don't read FROM database yet (still use in-memory mock)
- ❌ Services don't write TO database (changes not persisted)
- ❌ No database-backed implementation of `IMockDataService`

The database infrastructure is in place, but the services still use in-memory data. To fully use the database, you'd need to either:
1. Create a `DatabaseDataService` implementing `IMockDataService`
2. Or modify `MockDataService` to optionally use the database

---

## Entity Model Mapping Notes

The domain models have nested structures that are flattened to JSON in entities:

### User
- Model: `Name`, `Role` (enum), `Preferences` (object)
- Entity: `Name`, `Role` (enum), `PreferencesJson` (JSON)

### RepositoryInfo
- Model: `RepositoryId`, `Stack` (nested), `Commits` (nested), `Readme` (nested)
- Entity: `Id`, `RepositoryId`, `StackJson`, `CommitsJson`, `ReadmeJson`

### SyncJobInfo
- Model: `ErrorCount`, no `Errors` list
- Entity: `ErrorCount`

### TaskDocumentation
- Model: `Instructions`, `SystemGuidance`, `RelatedLinks`, `EstimatedDuration`, etc.
- Entity: `InstructionsJson`, `SystemGuidanceJson`, etc.

---

## Next Steps (If Continuing)

1. **Full Database Integration**
   - Create `DatabaseDataService` that implements `IMockDataService` using EF Core
   - Register conditionally based on `UseDatabase` setting
   - Keep `MockDataService` as fallback

2. **Repository CRUD**
   - Add repository info to database seeder
   - Implement repo sync from Azure DevOps to database

3. **Audit Logging to Database**
   - Wire `AuditService` to save to `AuditLogs` table

---

## Quick Reference

### Connection String
```
Server=localhost,1433;Database=LifecycleDashboard;User Id=sa;Password=LifecycleDev123!;TrustServerCertificate=True;Encrypt=False
```

### Check Docker Status
```bash
docker ps
```

### Check Database Tables (if sqlcmd available)
```bash
docker exec lifecycle-sql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'LifecycleDev123!' -d LifecycleDashboard -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES"
```

### Build & Test
```bash
dotnet build
dotnet run
```
