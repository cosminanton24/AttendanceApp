param (
    [string]$MigrationName = "InitialCreate"
)

Write-Host "Running EF migration: $MigrationName" -ForegroundColor Cyan

dotnet ef migrations add $MigrationName --project .\AttendanceApp.Infrastructure\AttendanceApp.Infrastructure.csproj --startup-project .\AttendanceApp.Web\AttendanceApp.Web.csproj
  
dotnet ef database update --project .\AttendanceApp.Infrastructure\AttendanceApp.Infrastructure.csproj --startup-project .\AttendanceApp.Web\AttendanceApp.Web.csproj