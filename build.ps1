#requires -version 5

<#
.SYNOPSIS
Builds this repository.
.DESCRIPTION
This script runs MSBuild on this repository.
.PARAMETER Configuration
Specify MSBuild configuration: Debug, Release
.PARAMETER Pack
Produce NuGet packages.
.PARAMETER Sign
Sign assemblies and NuGet packages (requires additional configuration not provided by this script).
.PARAMETER Verbosity
MSBuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]
.PARAMETER MSBuildArguments
Additional MSBuild arguments to be passed through.
.EXAMPLE
Building release packages.
    build.ps1 -Configuration Release -Pack /p:some_param=some_value
#>
[CmdletBinding(PositionalBinding = $false, DefaultParameterSetName='Groups')]
param(
    [ValidateSet('Debug', 'Release')]$Configuration,

    [switch]$Pack,

    [switch]$Sign,
    
    [string]$Verbosity = 'minimal',

    [switch]$Help,

    # Remaining arguments will be passed to MSBuild directly
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$MSBuildArguments
)

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'
$SolutionFile = "$PSScriptRoot/DataCore.Adapter.sln"
$Artifacts = "$PSScriptRoot/artifacts"

if ($Help) {
    Get-Help $PSCommandPath
    exit 1
}

. "$PSScriptRoot/build/tools.ps1"

# Set MSBuild verbosity
$MSBuildArguments += "/v:$Verbosity"

# Select targets
$MSBuildTargets = @()
$MSBuildTargets += 'Build'
if ($Pack) {
    $MSBuildTargets += 'Pack'
}

$local:targets = [string]::Join(';',$MSBuildTargets)
$MSBuildArguments += "/t:$targets"

# Set default configuration if required
if (-not $Configuration) {
    $Configuration = 'Debug'
}
$MSBuildArguments += "/p:Configuration=$Configuration"

# If the Sign flag is set, add a SignOutput build argument.
if ($Sign) {
    $MSBuildArguments += "/p:SignOutput=true"
}

$local:exit_code = $null
try {
    # Clear artifacts folder
    Clear-Artifacts

    # Run the build
    Run-Build
}
catch {
    Write-Host $_.ScriptStackTrace
    $exit_code = 1
}
finally {
    if (! $exit_code) {
        $exit_code = $LASTEXITCODE
    }
}

exit $exit_code