<#
.SYNOPSIS
  Restaura la base de datos de VetClinic desde un backup generado por backup-db.ps1.

.DESCRIPTION
  - Copia el archivo .dump al contenedor y lo restaura con pg_restore.
  - Usa --clean --if-exists para reemplazar el contenido actual de la base.
  - Requisito: el contenedor de PostgreSQL debe estar levantado.

.EJEMPLO
  .\restore-db.ps1 -BackupFile D:\vetclinic\backups\vetclinic_20260923_020000.dump
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFile,
    [string]$Container = "vetclinic-postgres",
    [string]$DbUser = "vetapp",
    [string]$DbName = "vetclinic"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $BackupFile)) {
    throw "No existe el archivo de backup: $BackupFile"
}

$remoteName = Split-Path $BackupFile -Leaf
docker cp $BackupFile "${Container}:/tmp/$remoteName"
if ($LASTEXITCODE -ne 0) { throw "docker cp fallo (exit $LASTEXITCODE)" }

Write-Host "Restaurando en $DbName desde $remoteName ..."
docker exec $Container pg_restore -U $DbUser -d $DbName --clean --if-exists --no-owner "/tmp/$remoteName"
$restoreCode = $LASTEXITCODE

docker exec $Container rm -f "/tmp/$remoteName"

if ($restoreCode -ne 0) {
    throw "pg_restore fallo con exit code $restoreCode (revisa si la base tiene conexiones activas)."
}

Write-Host "Base restaurada correctamente desde: $BackupFile"