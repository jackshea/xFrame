param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Args
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$python = "python"

try {
    & $python --version | Out-Null
}
catch {
    throw "python is required to run validate.ps1 wrapper."
}

& $python "$scriptDir/validate.py" @Args
exit $LASTEXITCODE
