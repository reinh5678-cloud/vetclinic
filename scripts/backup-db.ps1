<#
.SYNOPSIS
  Genera un backup de la base de datos de VetClinic (PostgreSQL en Docker)
  usando pg_dump en formato custom y lo copia a una carpeta local.

.DESCRIPTION
  - Ejecuta pg_dump (formato -Fc, comprimido) dentro del contenedor.
  - Copia el archivo a $BackupDir (por defecto ..\backups relativo al script).
  - Conserva solo los backups de los ultimos $RetentionDays dias.
  - Se puede programar con el Programador de tareas de Windows:
      schtasks /create /tn "VetClinicBackup" /tr "powershell -ExecutionPolicy Bypass -File D:\vetclinic\scripts\backup-db.ps1" /sc daily /st 02:00
#>

param(
    [string]$Container = "vetclinic-postgres",
    [string]$DbUser = "vetapp",
    [string]$DbName = "vetclinic",
    [string]$BackupDir = (Join-Path $PSScriptRoot "..\backups"),
    [int]$RetentionDays = 14
)

$ErrorActionPreference = "Stop"

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$fileName = "vetclinic_$timestamp.dump"
$absoluteDir = [System.IO.Path]::GetFullPath($BackupDir)
$absolutePath = Join-Path $absoluteDir $fileName

if (-not (Test-Path $absoluteDir)) {
    New-Item -ItemType Directory -Path $absoluteDir -Force | Out-Null
}

Write-Host "Generando backup de $DbName ($DbUser) en contenedor $Container ..."
docker exec $Container pg_dump -U $DbUser -d $DbName -Fc -f "/tmp/$fileName"
if ($LASTEXITCODE -ne 0) { throw "pg_dump fallo (exit $LASTEXITCODE)" }

docker cp "${Container}:/tmp/$fileName" $absolutePath
if ($LASTEXITCODE -ne 0) { throw "docker cp fallo (exit $LASTEXITCODE)" }

docker exec $Container rm -f "/tmp/$fileName"

$removed = Get-ChildItem -Path $absoluteDir -Filter "vetclinic_*.dump" -ErrorAction SilentlyContinue |
    Where-Object { $_.CreationTime -lt (Get-Date).AddDays(-$RetentionDays) } |
    Remove-Item -Force

Write-Host "Backup creado: $absolutePath"
Write-Host "Retencion: se conservan backups de los ultimos $RetentionDays dias."