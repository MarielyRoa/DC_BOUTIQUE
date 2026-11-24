@echo off
chcp 65001 > nul
title Instalador de DC_BOUTIQUE

echo =========================================================================
echo                 INSTALADOR OFICIAL DE DC_BOUTIQUE
echo        Sistema de Punto de Venta y Gestión para Dahiana Corporan
echo =========================================================================
echo.

:: 1. Verificación de permisos de Administrador
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [SOLICITANDO PERMISOS] Se requieren privilegios de Administrador para instalar...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

set "INSTALL_DIR=C:\Program Files\DC_BOUTIQUE"
set "DATA_BASE=C:\ProgramData\DC_BOUTIQUE"
set "DATA_DIR=%DATA_BASE%\Data"
set "BACKUP_DIR=%DATA_BASE%\Backups"
set "IMAGES_DIR=%DATA_BASE%\Images\productos"
set "DB_FILE=%DATA_DIR%\DC_BOUTIQUE.db"
set "SOURCE_DIR=%~dp0publish"

if not exist "%SOURCE_DIR%\DC_BOUTIQUEAPP.exe" (
    if exist "%~dp0DC_BOUTIQUEAPP.exe" (
        set "SOURCE_DIR=%~dp0"
    ) else (
        echo [ERROR] No se encontraron los archivos del programa para instalar.
        echo Asegúrese de ejecutar primero "Publicar_Y_Compilar.bat" o colocar los archivos en la carpeta del instalador.
        echo.
        pause
        exit /b 1
    )
)

echo [1/5] Creando directorios permanentes en ProgramData...
if not exist "%DATA_DIR%" mkdir "%DATA_DIR%"
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"
if not exist "%IMAGES_DIR%" mkdir "%IMAGES_DIR%"

echo [2/5] Configurando permisos de lectura y escritura para usuarios de Windows...
icacls "%DATA_BASE%" /grant *S-1-5-32-545:(OI)(CI)M /T /Q >nul 2>&1
icacls "%DATA_BASE%" /grant "Usuarios":(OI)(CI)M /T /Q >nul 2>&1
icacls "%DATA_BASE%" /grant "Users":(OI)(CI)M /T /Q >nul 2>&1

echo [3/5] Verificando base de datos SQLite...
if exist "%DB_FILE%" (
    echo       -> Base de datos existente encontrada en: %DB_FILE%
    echo       -> SE CONSERVARÁN TODOS LOS DATOS PREVIOS INTACTOS.
) else (
    echo       -> Primera instalación: la base de datos se inicializará automáticamente
    echo          con los usuarios base y configuración al iniciar el sistema.
)

echo [4/5] Instalando archivos del programa en "%INSTALL_DIR%"...
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
robocopy "%SOURCE_DIR%" "%INSTALL_DIR%" /E /IS /IT /NP /NJH /NJS >nul

echo [5/5] Creando accesos directos en el Escritorio y Menú Inicio...
powershell -NoProfile -Command ^
  "$ws = New-Object -ComObject WScript.Shell; " ^
  "$desk = [Environment]::GetFolderPath('Desktop'); " ^
  "$sDesk = $ws.CreateShortcut(\"$desk\DC BOUTIQUE.lnk\"); " ^
  "$sDesk.TargetPath = '%INSTALL_DIR%\DC_BOUTIQUEAPP.exe'; " ^
  "$sDesk.WorkingDirectory = '%INSTALL_DIR%'; " ^
  "$sDesk.IconLocation = '%INSTALL_DIR%\wwwroot\favicon.ico'; " ^
  "$sDesk.Description = 'DC Boutique - Punto de Venta y Gestión'; " ^
  "$sDesk.Save(); " ^
  "$progs = [Environment]::GetFolderPath('CommonPrograms'); " ^
  "$sProg = $ws.CreateShortcut(\"$progs\DC BOUTIQUE.lnk\"); " ^
  "$sProg.TargetPath = '%INSTALL_DIR%\DC_BOUTIQUEAPP.exe'; " ^
  "$sProg.WorkingDirectory = '%INSTALL_DIR%'; " ^
  "$sProg.IconLocation = '%INSTALL_DIR%\wwwroot\favicon.ico'; " ^
  "$sProg.Description = 'DC Boutique - Punto de Venta y Gestión'; " ^
  "$sProg.Save();"

echo.
echo =========================================================================
echo                  ¡INSTALACIÓN COMPLETADA CON ÉXITO!
echo.
echo   • Programa:       %INSTALL_DIR%
echo   • Base de Datos:  %DB_FILE%
echo   • Respaldos:      %BACKUP_DIR%
echo   • Acceso Directo: Creado en el Escritorio
echo.
echo   El sistema es 100%% autónomo: no requiere SQL Server ni internet.
echo =========================================================================
echo.

set /p RESP="¿Desea iniciar DC_BOUTIQUE ahora mismo? (S/N): "
if /i "%RESP%"=="S" (
    start "" "%INSTALL_DIR%\DC_BOUTIQUEAPP.exe"
)

exit /b 0
