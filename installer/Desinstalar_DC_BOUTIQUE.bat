@echo off
chcp 65001 > nul
title Desinstalador de DC_BOUTIQUE

echo =========================================================================
echo                 DESINSTALADOR DE DC_BOUTIQUE
echo =========================================================================
echo.

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [SOLICITANDO PERMISOS] Se requieren privilegios de Administrador para desinstalar...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

set "INSTALL_DIR=C:\Program Files\DC_BOUTIQUE"
set "DATA_DIR=C:\ProgramData\DC_BOUTIQUE"

echo [1/3] Cerrando procesos de DC_BOUTIQUE si están en ejecución...
taskkill /f /im DC_BOUTIQUEAPP.exe >nul 2>&1

echo [2/3] Eliminando accesos directos...
powershell -NoProfile -Command ^
  "$desk = [Environment]::GetFolderPath('Desktop'); " ^
  "Remove-Item \"$desk\DC BOUTIQUE.lnk\" -Force -ErrorAction SilentlyContinue; " ^
  "$progs = [Environment]::GetFolderPath('CommonPrograms'); " ^
  "Remove-Item \"$progs\DC BOUTIQUE.lnk\" -Force -ErrorAction SilentlyContinue;"

echo [3/3] Eliminando archivos de la aplicación en "%INSTALL_DIR%"...
if exist "%INSTALL_DIR%" rmdir /s /q "%INSTALL_DIR%"

echo.
echo =========================================================================
echo La aplicación ha sido desinstalada de Program Files.
echo.
echo Su base de datos y respaldos se encuentran seguros en:
echo %DATA_DIR%
echo.
set /p DELDATA="¿Desea ELIMINAR permanentemente la base de datos y respaldos? (Escriba BORRAR para confirmar, o presione Enter para conservarlos): "
if "%DELDATA%"=="BORRAR" (
    rmdir /s /q "%DATA_DIR%"
    echo Base de datos y respaldos eliminados.
) else (
    echo La base de datos y respaldos fueron conservados.
)
echo =========================================================================
echo.
pause
