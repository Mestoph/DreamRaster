# Copyright (C) 2026 Mestoph
# SPDX-License-Identifier: AGPL-3.0-or-later

#requires -Version 5.1
<#
FR : Diagnostic de compilation bilingue pour DreamRaster.
     Le script lance restore + build, capture stdout/stderr, extrait erreurs et
     avertissements puis produit un rapport Markdown et un rapport texte.
EN: Bilingual build diagnostic for DreamRaster.
    The script runs restore + build, captures stdout/stderr, extracts errors and
    warnings, then writes Markdown and plain-text reports.
#>

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$Project = "src/DreamRaster/DreamRaster.csproj",

    [switch]$NoOpenReport
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

function Write-Section {
    param([string]$Text)
    Write-Host ""
    Write-Host ("=" * 72)
    Write-Host "  $Text"
    Write-Host ("=" * 72)
}

function Invoke-CapturedCommand {
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [Parameter(Mandatory)]
        [string]$LogFile
    )

    $start = Get-Date
    $allLines = New-Object System.Collections.Generic.List[string]

    Write-Host ""
    Write-Host ">> $Name $($Arguments -join ' ')"
    Write-Host ""

    # FR : L'opérateur d'appel PowerShell fonctionne avec Windows PowerShell 5.1
    #      et PowerShell 7 sous Windows/Linux, contrairement à ArgumentList qui
    #      n'est pas disponible sur tous les runtimes .NET Framework.
    # EN: PowerShell's invocation operator works with Windows PowerShell 5.1
    #     and PowerShell 7 on Windows/Linux, unlike ArgumentList on older .NET Framework.
    & $Name @Arguments 2>&1 | ForEach-Object {
        $line = $_.ToString()
        $allLines.Add($line)
        Write-Host $line
    }

    $exitCode = $LASTEXITCODE
    $duration = (Get-Date) - $start

    $allLines | Set-Content -LiteralPath $LogFile -Encoding UTF8

    [pscustomobject]@{
        ExitCode = $exitCode
        Duration = $duration
        Lines = @($allLines)
        LogFile = $LogFile
    }
}

function Parse-DotNetIssues {
    param([string[]]$Lines)

    $items = New-Object System.Collections.Generic.List[object]

    # Standard compiler/MSBuild format:
    # path(line,col): error CSxxxx: message [project]
    # path(line,col): warning CSxxxx: message [project]
    $patternWithFile = '^(?<file>.+?)\((?<line>\d+),(?<col>\d+)\):\s*(?<severity>error|warning)\s+(?<code>[A-Za-z]+\d+):\s*(?<message>.*?)(?:\s+\[(?<project>[^\]]+)\])?$'

    # Restore / MSBuild messages can omit file and coordinates.
    $patternGeneric = '^(?<prefix>.*?)\b(?<severity>error|warning)\s+(?<code>(?:CS|NU|MSB|NETSDK|CA|SYSLIB)\d+):\s*(?<message>.*)$'

    foreach ($lineText in $Lines) {
        if ([string]::IsNullOrWhiteSpace($lineText)) {
            continue
        }

        $match = [regex]::Match(
            $lineText,
            $patternWithFile,
            [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

        if ($match.Success) {
            $items.Add([pscustomobject]@{
                Severity = $match.Groups["severity"].Value.ToLowerInvariant()
                Code = $match.Groups["code"].Value
                File = $match.Groups["file"].Value.Trim()
                Line = [int]$match.Groups["line"].Value
                Column = [int]$match.Groups["col"].Value
                Message = $match.Groups["message"].Value.Trim()
                Raw = $lineText
            })
            continue
        }

        $match = [regex]::Match(
            $lineText,
            $patternGeneric,
            [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

        if ($match.Success) {
            $items.Add([pscustomobject]@{
                Severity = $match.Groups["severity"].Value.ToLowerInvariant()
                Code = $match.Groups["code"].Value
                File = $match.Groups["prefix"].Value.Trim()
                Line = 0
                Column = 0
                Message = $match.Groups["message"].Value.Trim()
                Raw = $lineText
            })
        }
    }

    # Dotnet often prints each issue twice: once during compilation and once in summary.
    # Keep one logical entry per severity/code/file/line/message.
    @(
        $items |
        Sort-Object Severity, Code, File, Line, Column, Message -Unique
    )
}

function MdEscape {
    param([AllowNull()][string]$Value)

    if ($null -eq $Value) {
        return ""
    }

    $Value.Replace("|", "\|").Replace("`r", " ").Replace("`n", " ")
}

function HtmlEncode {
    param([AllowNull()][string]$Value)

    if ($null -eq $Value) {
        return ""
    }

    [System.Net.WebUtility]::HtmlEncode($Value)
}

function HtmlIssueRows {
    param(
        [object[]]$Items,
        [string]$CssClass
    )

    $rows = New-Object System.Text.StringBuilder

    foreach ($issue in $Items) {
        $file = if ([string]::IsNullOrWhiteSpace($issue.File)) {
            "—"
        } else {
            HtmlEncode $issue.File
        }

        $line = if ($issue.Line -gt 0) {
            $issue.Line.ToString()
        } else {
            "—"
        }

        $column = if ($issue.Column -gt 0) {
            $issue.Column.ToString()
        } else {
            "—"
        }

        [void]$rows.AppendLine(
            "<tr class='$CssClass'><td><code>$(HtmlEncode $issue.Code)</code></td><td class='path'>$file</td><td>$line</td><td>$column</td><td>$(HtmlEncode $issue.Message)</td></tr>")
    }

    $rows.ToString()
}

$projectRoot =
    [System.IO.Path]::GetFullPath(
        (Join-Path $PSScriptRoot "../.."))

Set-Location $projectRoot

$projectPath = Join-Path $projectRoot $Project
$reportRoot = Join-Path $projectRoot "build-reports"

$stamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$runDir = Join-Path $reportRoot $stamp
New-Item -ItemType Directory -Path $runDir -Force | Out-Null

$restoreLog = Join-Path $runDir "restore.log"
$buildLog = Join-Path $runDir "build.log"
$rawLog = Join-Path $runDir "full-build.log"
$mdReport = Join-Path $runDir "BUILD_REPORT_FR_EN.md"
$txtReport = Join-Path $runDir "BUILD_REPORT_FR_EN.txt"
$htmlReport = Join-Path $runDir "BUILD_REPORT_FR_EN.html"

$latestMd = Join-Path $reportRoot "LATEST_BUILD_REPORT_FR_EN.md"
$latestTxt = Join-Path $reportRoot "LATEST_BUILD_REPORT_FR_EN.txt"
$latestHtml = Join-Path $reportRoot "LATEST_BUILD_REPORT_FR_EN.html"
$latestLog = Join-Path $reportRoot "LATEST_BUILD_RAW.log"

Write-Section "DreamRaster - Diagnostic compilation / Build diagnostic"
Write-Host "Projet / Project       : $projectPath"
Write-Host "Configuration          : $Configuration"
Write-Host "Rapports / Reports     : $runDir"

if (-not (Test-Path -LiteralPath $projectPath)) {
    $msg = @"
ERREUR / ERROR
Le projet est introuvable / Project not found:
$projectPath
"@
    $msg | Set-Content -LiteralPath $txtReport -Encoding UTF8
    $msg | Set-Content -LiteralPath $latestTxt -Encoding UTF8
    Write-Host $msg -ForegroundColor Red
    exit 2
}

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue

if ($null -eq $dotnetCommand) {
    $msg = @"
ERREUR / ERROR

FR : Le SDK .NET est introuvable dans PATH.
     Installez le SDK .NET 9 puis relancez scripts/windows/build-diagnostic.bat.

EN: The .NET SDK cannot be found in PATH.
    Install the .NET 9 SDK, then run scripts/windows/build-diagnostic.bat again.
"@
    $msg | Set-Content -LiteralPath $txtReport -Encoding UTF8
    $msg | Set-Content -LiteralPath $latestTxt -Encoding UTF8
    Write-Host $msg -ForegroundColor Red
    exit 3
}

$dotnetVersion = (& dotnet --version 2>&1 | Out-String).Trim()
$dotnetInfo = (& dotnet --info 2>&1 | Out-String).Trim()
$osDescription = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
$processArch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()

Write-Section "1/2 Restore / Restauration"

$restoreResult = Invoke-CapturedCommand `
    -Name "dotnet" `
    -Arguments @("restore", $Project, "--nologo") `
    -LogFile $restoreLog

$buildResult = $null

if ($restoreResult.ExitCode -eq 0) {
    Write-Section "2/2 Build / Compilation"

    $buildResult = Invoke-CapturedCommand `
        -Name "dotnet" `
        -Arguments @(
            "build",
            $Project,
            "-c", $Configuration,
            "--no-restore",
            "--nologo",
            "--verbosity", "minimal"
        ) `
        -LogFile $buildLog
}
else {
    Write-Host ""
    Write-Host "FR : La restauration a echoue, la compilation n'est pas lancee." -ForegroundColor Red
    Write-Host "EN: Restore failed, therefore build was not started." -ForegroundColor Red
}

$combinedLines = New-Object System.Collections.Generic.List[string]
$combinedLines.Add("========== RESTORE ==========")
foreach ($line in $restoreResult.Lines) { $combinedLines.Add($line) }

if ($null -ne $buildResult) {
    $combinedLines.Add("")
    $combinedLines.Add("========== BUILD ==========")
    foreach ($line in $buildResult.Lines) { $combinedLines.Add($line) }
}

$combinedLines | Set-Content -LiteralPath $rawLog -Encoding UTF8

$issues = Parse-DotNetIssues -Lines @($combinedLines)
$errors = @($issues | Where-Object Severity -eq "error")
$warnings = @($issues | Where-Object Severity -eq "warning")

$restoreOk = $restoreResult.ExitCode -eq 0
$buildStarted = $null -ne $buildResult
$buildOk = $buildStarted -and $buildResult.ExitCode -eq 0
$overallOk = $restoreOk -and $buildOk

$statusFr = if ($overallOk) {
    "SUCCES"
}
elseif (-not $restoreOk) {
    "ECHEC DE LA RESTAURATION"
}
else {
    "ECHEC DE LA COMPILATION"
}

$statusEn = if ($overallOk) {
    "SUCCESS"
}
elseif (-not $restoreOk) {
    "RESTORE FAILED"
}
else {
    "BUILD FAILED"
}

$generatedLocal = Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"

$md = New-Object System.Text.StringBuilder
[void]$md.AppendLine("# DreamRaster — Rapport de compilation / Build report")
[void]$md.AppendLine()
[void]$md.AppendLine("**FR : Résultat : $statusFr**  ")
[void]$md.AppendLine("**EN: Result: $statusEn**")
[void]$md.AppendLine()
[void]$md.AppendLine("| Information | Valeur / Value |")
[void]$md.AppendLine("|---|---|")
[void]$md.AppendLine("| Date | $(MdEscape $generatedLocal) |")
[void]$md.AppendLine("| Projet / Project | `$(MdEscape $projectPath)` |")
[void]$md.AppendLine("| Configuration | $Configuration |")
[void]$md.AppendLine("| SDK .NET | `$(MdEscape $dotnetVersion)` |")
[void]$md.AppendLine("| OS | $(MdEscape $osDescription) |")
[void]$md.AppendLine("| Architecture | $processArch |")
[void]$md.AppendLine("| Restore | $(if ($restoreOk) { 'OK' } else { "ECHEC / FAILED (code $($restoreResult.ExitCode))" }) |")
[void]$md.AppendLine("| Build | $(if (-not $buildStarted) { 'NON LANCE / NOT STARTED' } elseif ($buildOk) { 'OK' } else { "ECHEC / FAILED (code $($buildResult.ExitCode))" }) |")
[void]$md.AppendLine("| Erreurs / Errors | $($errors.Count) |")
[void]$md.AppendLine("| Avertissements / Warnings | $($warnings.Count) |")
[void]$md.AppendLine()

if ($errors.Count -gt 0) {
    [void]$md.AppendLine("## Erreurs / Errors")
    [void]$md.AppendLine()
    [void]$md.AppendLine("| Code | Fichier / File | Ligne / Line | Message |")
    [void]$md.AppendLine("|---|---|---:|---|")

    foreach ($issue in $errors) {
        $where = if ($issue.File) { MdEscape $issue.File } else { "—" }
        $lineNo = if ($issue.Line -gt 0) { $issue.Line.ToString() } else { "—" }

        [void]$md.AppendLine(
            "| `$($issue.Code)` | $where | $lineNo | $(MdEscape $issue.Message) |")
    }

    [void]$md.AppendLine()
}

if ($warnings.Count -gt 0) {
    [void]$md.AppendLine("## Avertissements / Warnings")
    [void]$md.AppendLine()
    [void]$md.AppendLine("| Code | Fichier / File | Ligne / Line | Message |")
    [void]$md.AppendLine("|---|---|---:|---|")

    foreach ($issue in $warnings) {
        $where = if ($issue.File) { MdEscape $issue.File } else { "—" }
        $lineNo = if ($issue.Line -gt 0) { $issue.Line.ToString() } else { "—" }

        [void]$md.AppendLine(
            "| `$($issue.Code)` | $where | $lineNo | $(MdEscape $issue.Message) |")
    }

    [void]$md.AppendLine()
}

if ($overallOk) {
    [void]$md.AppendLine("## Conclusion")
    [void]$md.AppendLine()
    [void]$md.AppendLine("FR : La restauration et la compilation se sont terminées avec succès.")
    [void]$md.AppendLine()
    [void]$md.AppendLine("EN: Restore and build completed successfully.")
}
else {
    [void]$md.AppendLine("## Actions conseillées / Suggested actions")
    [void]$md.AppendLine()
    [void]$md.AppendLine("FR :")
    [void]$md.AppendLine("1. Corrigez d'abord les lignes de la section **Erreurs**.")
    [void]$md.AppendLine("2. Utilisez le code `CSxxxx`, `NUxxxx`, `MSBxxxx` ou `NETSDKxxxx` pour identifier la catégorie.")
    [void]$md.AppendLine("3. Si l'erreur n'est pas claire, transmettez ce rapport et `full-build.log`.")
    [void]$md.AppendLine()
    [void]$md.AppendLine("EN:")
    [void]$md.AppendLine("1. Fix the entries in **Errors** first.")
    [void]$md.AppendLine("2. Use the `CSxxxx`, `NUxxxx`, `MSBxxxx`, or `NETSDKxxxx` code to identify the category.")
    [void]$md.AppendLine("3. If the error is unclear, provide this report together with `full-build.log`.")
}

[void]$md.AppendLine()
[void]$md.AppendLine("## Commandes / Commands")
[void]$md.AppendLine()
[void]$md.AppendLine("```powershell")
[void]$md.AppendLine("dotnet restore `"$Project`"")
[void]$md.AppendLine("dotnet build `"$Project`" -c $Configuration --no-restore --nologo")
[void]$md.AppendLine("```")
[void]$md.AppendLine()
[void]$md.AppendLine("## Fichiers générés / Generated files")
[void]$md.AppendLine()
[void]$md.AppendLine("- Rapport HTML / HTML report: `$htmlReport`")
[void]$md.AppendLine("- Rapport Markdown / Markdown report: `$mdReport`")
[void]$md.AppendLine("- Rapport texte / Text report: `$txtReport`")
[void]$md.AppendLine("- Log brut / Raw log: `$rawLog`")
[void]$md.AppendLine("- Restore log: `$restoreLog`")
if ($null -ne $buildResult) {
    [void]$md.AppendLine("- Build log: `$buildLog`")
}
[void]$md.AppendLine()
[void]$md.AppendLine("## Informations .NET / .NET information")
[void]$md.AppendLine()
[void]$md.AppendLine("```text")
[void]$md.AppendLine($dotnetInfo)
[void]$md.AppendLine("```")

$md.ToString() | Set-Content -LiteralPath $mdReport -Encoding UTF8

$txt = New-Object System.Text.StringBuilder
[void]$txt.AppendLine("OPENCode Local AI - RAPPORT DE COMPILATION / BUILD REPORT")
[void]$txt.AppendLine(("=" * 72))
[void]$txt.AppendLine("FR : Resultat : $statusFr")
[void]$txt.AppendLine("EN: Result: $statusEn")
[void]$txt.AppendLine()
[void]$txt.AppendLine("Date: $generatedLocal")
[void]$txt.AppendLine("Project / Projet: $projectPath")
[void]$txt.AppendLine("Configuration: $Configuration")
[void]$txt.AppendLine(".NET SDK: $dotnetVersion")
[void]$txt.AppendLine("Errors / Erreurs: $($errors.Count)")
[void]$txt.AppendLine("Warnings / Avertissements: $($warnings.Count)")
[void]$txt.AppendLine()

if ($errors.Count -gt 0) {
    [void]$txt.AppendLine("ERREURS / ERRORS")
    [void]$txt.AppendLine(("-" * 72))
    foreach ($issue in $errors) {
        $location = if ($issue.Line -gt 0) {
            "$($issue.File):$($issue.Line):$($issue.Column)"
        } else {
            $issue.File
        }
        [void]$txt.AppendLine("[$($issue.Code)] $location")
        [void]$txt.AppendLine("  $($issue.Message)")
        [void]$txt.AppendLine()
    }
}

if ($warnings.Count -gt 0) {
    [void]$txt.AppendLine("AVERTISSEMENTS / WARNINGS")
    [void]$txt.AppendLine(("-" * 72))
    foreach ($issue in $warnings) {
        $location = if ($issue.Line -gt 0) {
            "$($issue.File):$($issue.Line):$($issue.Column)"
        } else {
            $issue.File
        }
        [void]$txt.AppendLine("[$($issue.Code)] $location")
        [void]$txt.AppendLine("  $($issue.Message)")
        [void]$txt.AppendLine()
    }
}

[void]$txt.AppendLine("RAW LOG / LOG BRUT")
[void]$txt.AppendLine($rawLog)
[void]$txt.AppendLine()
[void]$txt.AppendLine("FR : Transmettez ce rapport et le log brut si une correction est necessaire.")
[void]$txt.AppendLine("EN: Provide this report and the raw log when a fix is needed.")

$txt.ToString() | Set-Content -LiteralPath $txtReport -Encoding UTF8

# --------------------------------------------------------------------------
# FR : Rapport HTML autonome avec résumé visuel et mise en évidence.
# EN: Standalone HTML report with visual summary and issue highlighting.
# --------------------------------------------------------------------------
$statusClass = if ($overallOk) { "ok" } else { "fail" }
$restoreClass = if ($restoreOk) { "ok" } else { "fail" }
$buildClass = if ($buildOk) { "ok" } elseif (-not $buildStarted) { "neutral" } else { "fail" }

$restoreText = if ($restoreOk) {
    "OK"
} else {
    "ECHEC / FAILED · code $($restoreResult.ExitCode)"
}

$buildText = if (-not $buildStarted) {
    "NON LANCE / NOT STARTED"
} elseif ($buildOk) {
    "OK"
} else {
    "ECHEC / FAILED · code $($buildResult.ExitCode)"
}

$errorRows = HtmlIssueRows -Items $errors -CssClass "error-row"
$warningRows = HtmlIssueRows -Items $warnings -CssClass "warning-row"

$errorSection = if ($errors.Count -gt 0) {
@"
<section class="panel panel-error">
  <div class="panel-heading">
    <h2>Erreurs / Errors</h2>
    <span class="pill pill-error">$($errors.Count)</span>
  </div>
  <div class="table-wrap">
    <table>
      <thead>
        <tr>
          <th>Code</th>
          <th>Fichier / File</th>
          <th>Ligne / Line</th>
          <th>Col. / Column</th>
          <th>Message</th>
        </tr>
      </thead>
      <tbody>
        $errorRows
      </tbody>
    </table>
  </div>
</section>
"@
} else {
@"
<section class="panel panel-ok">
  <div class="empty-state">
    <span class="empty-icon">✓</span>
    <div>
      <strong>Aucune erreur / No errors</strong>
      <div>FR : Aucune erreur de compilation détectée.</div>
      <div>EN: No build errors were detected.</div>
    </div>
  </div>
</section>
"@
}

$warningSection = if ($warnings.Count -gt 0) {
@"
<section class="panel panel-warning">
  <div class="panel-heading">
    <h2>Avertissements / Warnings</h2>
    <span class="pill pill-warning">$($warnings.Count)</span>
  </div>
  <div class="table-wrap">
    <table>
      <thead>
        <tr>
          <th>Code</th>
          <th>Fichier / File</th>
          <th>Ligne / Line</th>
          <th>Col. / Column</th>
          <th>Message</th>
        </tr>
      </thead>
      <tbody>
        $warningRows
      </tbody>
    </table>
  </div>
</section>
"@
} else {
@"
<section class="panel panel-ok">
  <div class="empty-state">
    <span class="empty-icon">✓</span>
    <div>
      <strong>Aucun avertissement / No warnings</strong>
      <div>FR : Aucun avertissement détecté.</div>
      <div>EN: No build warnings were detected.</div>
    </div>
  </div>
</section>
"@
}

$conclusionHtml = if ($overallOk) {
@"
<div class="callout success">
  <strong>Compilation réussie / Build successful</strong>
  <p>FR : Restore et build se sont terminés avec succès.</p>
  <p>EN: Restore and build completed successfully.</p>
</div>
"@
} else {
@"
<div class="callout danger">
  <strong>Action requise / Action required</strong>
  <p>FR : Corrigez d'abord les erreurs en rouge puis relancez scripts/windows/build-diagnostic.bat.</p>
  <p>EN: Fix the red errors first, then run scripts/windows/build-diagnostic.bat again.</p>
</div>
"@
}

$rawLogUri = [Uri]::new($rawLog).AbsoluteUri
$restoreLogUri = [Uri]::new($restoreLog).AbsoluteUri
$buildLogUri = if ($null -ne $buildResult) {
    [Uri]::new($buildLog).AbsoluteUri
} else {
    $null
}

$buildLogLink = if ($null -ne $buildLogUri) {
    "<a class='file-link' href='$buildLogUri'>build.log</a>"
} else {
    "<span class='muted'>build.log — non créé / not created</span>"
}

$html = @"
<!doctype html>
<html lang="fr">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>DreamRaster — Build Report</title>
<style>
:root {
  color-scheme: dark;
  --bg: #111318;
  --surface: #1b1e25;
  --surface2: #232833;
  --border: #343b48;
  --text: #f2f4f8;
  --muted: #aeb6c5;
  --green: #48b982;
  --green-bg: #173127;
  --red: #ff7585;
  --red-bg: #3a1f25;
  --amber: #ffc857;
  --amber-bg: #3a3020;
  --blue: #67a7ff;
  --shadow: 0 12px 32px rgba(0,0,0,.25);
}
* { box-sizing: border-box; }
body {
  margin: 0;
  background: linear-gradient(180deg, #111318 0%, #171a20 100%);
  color: var(--text);
  font-family: "Segoe UI", system-ui, -apple-system, sans-serif;
  line-height: 1.45;
}
.container {
  width: min(1280px, calc(100% - 32px));
  margin: 28px auto 48px;
}
.hero {
  padding: 28px;
  border: 1px solid var(--border);
  border-radius: 18px;
  background: var(--surface);
  box-shadow: var(--shadow);
}
.hero-top {
  display: flex;
  gap: 18px;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
}
h1 { margin: 0; font-size: clamp(24px, 4vw, 38px); }
.subtitle { color: var(--muted); margin-top: 6px; }
.status {
  display: inline-flex;
  align-items: center;
  gap: 9px;
  padding: 10px 16px;
  border-radius: 999px;
  font-weight: 700;
}
.status.ok { color: #b9f4d6; background: var(--green-bg); border: 1px solid #2d7652; }
.status.fail { color: #ffd2d8; background: var(--red-bg); border: 1px solid #8c3e49; }
.dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: currentColor;
  box-shadow: 0 0 14px currentColor;
}
.cards {
  margin-top: 20px;
  display: grid;
  grid-template-columns: repeat(4, minmax(160px, 1fr));
  gap: 14px;
}
.card {
  padding: 18px;
  border-radius: 14px;
  background: var(--surface2);
  border: 1px solid var(--border);
}
.card .label { color: var(--muted); font-size: 13px; }
.card .value { margin-top: 6px; font-size: 27px; font-weight: 800; }
.card.error .value { color: var(--red); }
.card.warning .value { color: var(--amber); }
.card.ok .value { color: var(--green); }
.timeline {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
  margin-top: 14px;
}
.stage {
  padding: 16px 18px;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: #181b21;
}
.stage.ok { border-left: 5px solid var(--green); }
.stage.fail { border-left: 5px solid var(--red); }
.stage.neutral { border-left: 5px solid #758095; }
.stage strong { display: block; margin-bottom: 4px; }
.stage span { color: var(--muted); }
.panel {
  margin-top: 18px;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 16px;
  overflow: hidden;
  box-shadow: var(--shadow);
}
.panel-error { border-color: #6f343d; }
.panel-warning { border-color: #665428; }
.panel-ok { border-color: #2f684d; }
.panel-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 18px 20px;
  border-bottom: 1px solid var(--border);
}
.panel-heading h2 { margin: 0; font-size: 20px; }
.pill {
  min-width: 36px;
  text-align: center;
  border-radius: 999px;
  padding: 5px 10px;
  font-weight: 800;
}
.pill-error { background: var(--red-bg); color: var(--red); }
.pill-warning { background: var(--amber-bg); color: var(--amber); }
.table-wrap { overflow-x: auto; }
table { width: 100%; border-collapse: collapse; }
th, td {
  padding: 12px 14px;
  border-bottom: 1px solid #2d333e;
  text-align: left;
  vertical-align: top;
}
th {
  position: sticky;
  top: 0;
  background: #20242c;
  color: #dbe0e9;
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: .04em;
}
.error-row { background: rgba(255, 91, 110, .055); }
.error-row:hover { background: rgba(255, 91, 110, .11); }
.warning-row { background: rgba(255, 200, 87, .045); }
.warning-row:hover { background: rgba(255, 200, 87, .09); }
.path {
  max-width: 430px;
  overflow-wrap: anywhere;
  color: #cbd4e3;
}
code {
  font-family: Consolas, "Cascadia Code", monospace;
  color: #dce7ff;
  background: #15181e;
  padding: 2px 6px;
  border-radius: 5px;
}
.empty-state {
  display: flex;
  gap: 15px;
  align-items: center;
  padding: 20px;
}
.empty-icon {
  width: 42px;
  height: 42px;
  border-radius: 50%;
  display: inline-grid;
  place-items: center;
  flex: 0 0 auto;
  background: var(--green-bg);
  color: var(--green);
  border: 1px solid #2d7652;
  font-size: 22px;
  font-weight: 900;
}
.meta-grid {
  display: grid;
  grid-template-columns: 190px 1fr;
}
.meta-grid > div {
  padding: 11px 14px;
  border-bottom: 1px solid #2d333e;
}
.meta-grid .key { color: var(--muted); background: #1f232a; }
.meta-grid .val { overflow-wrap: anywhere; }
.callout {
  margin-top: 18px;
  padding: 18px 20px;
  border-radius: 14px;
  border: 1px solid var(--border);
}
.callout.success { background: var(--green-bg); border-color: #2d7652; }
.callout.danger { background: var(--red-bg); border-color: #8c3e49; }
.callout p { margin: 7px 0 0; }
.files {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  padding: 18px 20px;
}
.file-link {
  color: #dcecff;
  text-decoration: none;
  border: 1px solid #405270;
  background: #1e2b3e;
  padding: 8px 11px;
  border-radius: 8px;
}
.file-link:hover { border-color: var(--blue); }
.muted { color: var(--muted); }
pre {
  margin: 0;
  padding: 18px 20px;
  overflow: auto;
  background: #12151a;
  color: #d6dbe5;
  font-family: Consolas, "Cascadia Code", monospace;
  font-size: 12px;
}
.footer {
  margin-top: 18px;
  text-align: center;
  color: var(--muted);
  font-size: 12px;
}
@media (max-width: 820px) {
  .cards { grid-template-columns: 1fr 1fr; }
  .timeline { grid-template-columns: 1fr; }
  .meta-grid { grid-template-columns: 1fr; }
  .meta-grid .key { border-bottom: 0; padding-bottom: 4px; }
  .meta-grid .val { padding-top: 4px; }
}
@media (max-width: 480px) {
  .cards { grid-template-columns: 1fr; }
  .container { width: min(100% - 18px, 1280px); margin-top: 12px; }
  .hero { padding: 18px; }
}
</style>
</head>
<body>
<main class="container">
  <section class="hero">
    <div class="hero-top">
      <div>
        <h1>DreamRaster</h1>
        <div class="subtitle">Rapport de compilation / Build report · $(HtmlEncode $generatedLocal)</div>
      </div>
      <div class="status $statusClass">
        <span class="dot"></span>
        <span>$statusFr / $statusEn</span>
      </div>
    </div>

    <div class="cards">
      <div class="card error">
        <div class="label">Erreurs / Errors</div>
        <div class="value">$($errors.Count)</div>
      </div>
      <div class="card warning">
        <div class="label">Avertissements / Warnings</div>
        <div class="value">$($warnings.Count)</div>
      </div>
      <div class="card $(if ($restoreOk) { 'ok' } else { 'error' })">
        <div class="label">Restore</div>
        <div class="value">$(if ($restoreOk) { 'OK' } else { 'FAIL' })</div>
      </div>
      <div class="card $(if ($buildOk) { 'ok' } elseif (-not $buildStarted) { '' } else { 'error' })">
        <div class="label">Build</div>
        <div class="value">$(if ($buildOk) { 'OK' } elseif (-not $buildStarted) { 'N/A' } else { 'FAIL' })</div>
      </div>
    </div>

    <div class="timeline">
      <div class="stage $restoreClass">
        <strong>1. Restore / Restauration</strong>
        <span>$(HtmlEncode $restoreText)</span>
      </div>
      <div class="stage $buildClass">
        <strong>2. Build / Compilation</strong>
        <span>$(HtmlEncode $buildText)</span>
      </div>
    </div>
  </section>

  $conclusionHtml

  $errorSection

  $warningSection

  <section class="panel">
    <div class="panel-heading">
      <h2>Environnement / Environment</h2>
    </div>
    <div class="meta-grid">
      <div class="key">Projet / Project</div><div class="val"><code>$(HtmlEncode $projectPath)</code></div>
      <div class="key">Configuration</div><div class="val">$(HtmlEncode $Configuration)</div>
      <div class="key">SDK .NET</div><div class="val"><code>$(HtmlEncode $dotnetVersion)</code></div>
      <div class="key">OS</div><div class="val">$(HtmlEncode $osDescription)</div>
      <div class="key">Architecture</div><div class="val">$(HtmlEncode $processArch)</div>
      <div class="key">Date</div><div class="val">$(HtmlEncode $generatedLocal)</div>
    </div>
  </section>

  <section class="panel">
    <div class="panel-heading">
      <h2>Fichiers générés / Generated files</h2>
    </div>
    <div class="files">
      <a class="file-link" href="$rawLogUri">full-build.log</a>
      <a class="file-link" href="$restoreLogUri">restore.log</a>
      $buildLogLink
      <span class="file-link">$(HtmlEncode ([IO.Path]::GetFileName($mdReport)))</span>
      <span class="file-link">$(HtmlEncode ([IO.Path]::GetFileName($txtReport)))</span>
      <span class="file-link">$(HtmlEncode ([IO.Path]::GetFileName($htmlReport)))</span>
    </div>
  </section>

  <section class="panel">
    <div class="panel-heading">
      <h2>Informations .NET / .NET information</h2>
    </div>
    <pre>$(HtmlEncode $dotnetInfo)</pre>
  </section>

  <div class="footer">
    FR : Rapport généré automatiquement par scripts/windows/build-diagnostic.bat ·
    EN: Report automatically generated by scripts/windows/build-diagnostic.bat
  </div>
</main>
</body>
</html>
"@

$html | Set-Content -LiteralPath $htmlReport -Encoding UTF8

Copy-Item -LiteralPath $mdReport -Destination $latestMd -Force
Copy-Item -LiteralPath $txtReport -Destination $latestTxt -Force
Copy-Item -LiteralPath $htmlReport -Destination $latestHtml -Force
Copy-Item -LiteralPath $rawLog -Destination $latestLog -Force

Write-Section "Rapport / Report"
Write-Host "FR : Resultat : $statusFr"
Write-Host "EN: Result: $statusEn"
Write-Host ""
Write-Host "Erreurs / Errors           : $($errors.Count)"
Write-Host "Avertissements / Warnings  : $($warnings.Count)"
Write-Host ""
Write-Host "HTML     : $htmlReport"
Write-Host "Markdown : $mdReport"
Write-Host "Text     : $txtReport"
Write-Host "Raw log  : $rawLog"

if (-not $NoOpenReport) {
    try {
        Start-Process $htmlReport
    }
    catch {
        Write-Host "Impossible d'ouvrir le rapport HTML / Could not open HTML report: $($_.Exception.Message)"

        try {
            Start-Process notepad.exe -ArgumentList "`"$txtReport`""
        }
        catch {
            Write-Host "Impossible d'ouvrir le rapport texte / Could not open text report: $($_.Exception.Message)"
        }
    }
}

if ($overallOk) {
    exit 0
}

exit 1
