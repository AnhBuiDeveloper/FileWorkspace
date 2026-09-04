[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$failures = [System.Collections.Generic.List[string]]::new()

function Test-Condition {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        $failures.Add($Message)
    }
}

function Get-RepositoryFile {
    param([string]$RelativePath)

    return Join-Path $repositoryRoot $RelativePath
}

function Get-SourceLineCount {
    param([string]$RelativePath)

    return (Get-Content -LiteralPath (Get-RepositoryFile $RelativePath)).Count
}

function Test-DoesNotMatch {
    param(
        [string]$RelativePath,
        [string]$Pattern,
        [string]$Message
    )

    $content = Get-Content -LiteralPath (Get-RepositoryFile $RelativePath) -Raw
    Test-Condition -Condition ($content -notmatch $Pattern) -Message $Message
}

function Test-Matches {
    param(
        [string]$RelativePath,
        [string]$Pattern,
        [string]$Message
    )

    $content = Get-Content -LiteralPath (Get-RepositoryFile $RelativePath) -Raw
    Test-Condition -Condition ($content -match $Pattern) -Message $Message
}

# SOLID: fixed dependency direction and clear host composition.
Test-Condition (Test-Path -LiteralPath (Get-RepositoryFile 'Configuration\EnvironmentFileConfigurationExtensions.cs')) 'Missing Configuration boundary.'
Test-Condition (Test-Path -LiteralPath (Get-RepositoryFile 'Endpoints\FileWorkspaceEndpointExtensions.cs')) 'Missing Endpoints boundary.'
Test-Condition (Test-Path -LiteralPath (Get-RepositoryFile 'Models\ApiContracts.cs')) 'Missing Models boundary.'
Test-Condition (Test-Path -LiteralPath (Get-RepositoryFile 'Services\FileManagerService.cs')) 'Missing FileManagerService boundary.'
Test-Condition (Test-Path -LiteralPath (Get-RepositoryFile 'Services\UploadTokenValidator.cs')) 'Missing UploadTokenValidator boundary.'

Test-DoesNotMatch 'Models\ApiContracts.cs' 'using\s+FileWorkspace\.(Endpoints|Services)' 'Models must not depend on Endpoints or Services.'
Test-DoesNotMatch 'Services\FileManagerService.cs' 'using\s+FileWorkspace\.Endpoints' 'Services must not depend on Endpoints.'
Test-DoesNotMatch 'Services\UploadTokenValidator.cs' 'using\s+FileWorkspace\.Endpoints' 'Services must not depend on Endpoints.'
Test-DoesNotMatch 'Configuration\EnvironmentFileConfigurationExtensions.cs' 'using\s+FileWorkspace\.(Endpoints|Services)' 'Configuration must not depend on Endpoints or Services.'
Test-DoesNotMatch 'Endpoints\FileWorkspaceEndpointExtensions.cs' 'new\s+(FileManagerService|UploadTokenValidator)\s*\(' 'Endpoints must receive services through dependency injection.'

# KISS: Program is only composition; complex behavior belongs in the boundaries above.
Test-Condition ((Get-SourceLineCount 'Program.cs') -le 45) 'Program.cs is too large; move behavior to an appropriate boundary.'
Test-DoesNotMatch 'Program.cs' '\.Map(Get|Post|Put|Delete)\s*\(' 'Program.cs must delegate HTTP routes to Endpoints.'

# DRY: shared endpoint behavior stays centralized.
Test-Matches 'Endpoints\FileWorkspaceEndpointExtensions.cs' 'private\s+static\s+bool\s+IsAuthorized' 'Endpoint authorization must remain centralized in IsAuthorized.'
Test-Matches 'Endpoints\FileWorkspaceEndpointExtensions.cs' 'private\s+static\s+IResult\s+Error' 'Endpoint error responses must remain centralized in Error.'

# YAGNI: keep the current application dependency-free until a concrete requirement justifies a package.
$projectContent = Get-Content -LiteralPath (Get-RepositoryFile 'FileWorkspace.csproj') -Raw
Test-Condition ($projectContent -notmatch '<PackageReference\b') 'Adding a production package requires an approved requirement and an architecture-check update.'

# Protect the test baseline for the two stateful application services and the HTTP boundary.
Test-Condition (Test-Path -LiteralPath (Get-RepositoryFile 'FileWorkspace.Tests\Services\FileManagerServiceTests.cs')) 'FileManagerService requires service tests.'
Test-Condition (Test-Path -LiteralPath (Get-RepositoryFile 'FileWorkspace.Tests\Services\UploadTokenValidatorTests.cs')) 'UploadTokenValidator requires service tests.'
Test-Condition (Test-Path -LiteralPath (Get-RepositoryFile 'FileWorkspace.Tests\Endpoints\FileManagerApiTests.cs')) 'HTTP endpoints require API integration tests.'

# Durable documentation is part of the quality contract.
foreach ($governanceFile in @('AGENTS.md', 'CONTRIBUTING.md', '.github\copilot-instructions.md', '.github\PULL_REQUEST_TEMPLATE.md', 'CODEOWNERS')) {
    Test-Condition (Test-Path -LiteralPath (Get-RepositoryFile $governanceFile)) "Missing contributor governance file: $governanceFile."
}
Test-Matches 'AGENTS.md' 'PROJECT-MEMORY\.md' 'AGENTS.md must require reading PROJECT-MEMORY.md.'
Test-Matches 'AGENTS.md' 'UI-STANDARDS\.md' 'AGENTS.md must require UI-STANDARDS.md for browser work.'
Test-Matches 'AGENTS.md' 'SOLID.{0,8}KISS.{0,8}DRY.{0,8}YAGNI' 'AGENTS.md must require engineering principles.'
Test-Matches 'AGENTS.md' 'dotnet test' 'AGENTS.md must require .NET tests.'
Test-Matches 'AGENTS.md' 'npm run test:ui' 'AGENTS.md must require Playwright tests.'
Test-Matches 'AGENTS.md' 'alert\(\).*confirm\(\).*prompt\(' 'AGENTS.md must prohibit browser-native dialogs.'
Test-DoesNotMatch 'wwwroot\site.js' '\b(?:window\.)?(alert|confirm|prompt)\s*\(' 'Browser-native dialogs are prohibited; use an accessible application modal.'

foreach ($principle in @('SOLID', 'KISS', 'DRY', 'YAGNI')) {
    Test-Matches 'PROJECT-MEMORY.md' $principle "PROJECT-MEMORY.md must document $principle."
}
foreach ($width in @('320 px', '375 px', '430 px', '768 px', '1024 px', '1440 px')) {
    Test-Matches 'UI-STANDARDS.md' $width "UI-STANDARDS.md must include the $width viewport check."
}
Test-Matches 'UI-STANDARDS.md' 'Browser-provided.*alert.*confirm.*prompt' 'UI-STANDARDS.md must prohibit browser-provided dialogs.'

if ($failures.Count -gt 0) {
    Write-Error ('Architecture quality check failed:' + [Environment]::NewLine + ($failures | ForEach-Object { "- $_" } | Out-String))
    exit 1
}

Write-Host 'Architecture quality check passed.'
