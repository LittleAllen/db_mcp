# PostgreSQL MCP Server Test Script

Write-Host "PostgreSQL MCP Server Build Test" -ForegroundColor Green

# Check for .NET 8.0
Write-Host "Checking .NET version..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "Found .NET version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "Error: .NET not found. Please install .NET 8.0 or later." -ForegroundColor Red
    exit 1
}

# Build project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build DbMcpServer.csproj --configuration Release
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
} else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Check required dependencies
Write-Host "Checking dependencies..." -ForegroundColor Yellow
$packages = @("Npgsql", "ModelContextProtocol")
foreach ($package in $packages) {
    $found = Select-String -Path "DbMcpServer.csproj" -Pattern $package
    if ($found) {
        Write-Host "✓ $package package installed" -ForegroundColor Green
    } else {
        Write-Host "✗ $package package not found" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Setup Instructions:" -ForegroundColor Cyan
Write-Host "1. Ensure you have a running PostgreSQL database"
Write-Host "2. Update connection string in appsettings.json"
Write-Host "3. Run command: dotnet run"
Write-Host ""
Write-Host "For detailed setup see README_PostgreSQL.md" -ForegroundColor Cyan
