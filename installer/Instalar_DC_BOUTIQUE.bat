@echo off
title Instalador de DC_BOUTIQUE

echo =========================================================================
echo                 INSTALADOR OFICIAL DE DC_BOUTIQUE
echo        Sistema de Punto de Venta y Gestion para Dahiana Corporan
echo =========================================================================
echo.

:: 1. Verificacion de permisos de Administrador
net session >nul 2>&1
if errorlevel 1 goto :PEDIR_ADMIN
goto :INICIAR_INSTALACION

:PEDIR_ADMIN
echo [SOLICITANDO PERMISOS] Se requieren privilegios de Administrador para instalar...
powershell -NoProfile -Command "Start-Process '%~f0' -Verb RunAs"
exit /b

:INICIAR_INSTALACION
set "INSTALL_DIR=C:\Program Files\DC_BOUTIQUE"
set "DATA_BASE=C:\ProgramData\DC_BOUTIQUE"
set "DATA_DIR=%DATA_BASE%\Data"
set "BACKUP_DIR=%DATA_BASE%\Backups"
set "IMAGES_DIR=%DATA_BASE%\Images\productos"
set "DB_FILE=%DATA_DIR%\DC_BOUTIQUE.db"
set "SOURCE_DIR=%~dp0publish"

if exist "%SOURCE_DIR%\DC_BOUTIQUEAPP.exe" goto :ORIGEN_OK
if exist "%~dp0DC_BOUTIQUEAPP.exe" set "SOURCE_DIR=%~dp0" & goto :ORIGEN_OK

echo [ERROR] No se encontraron los archivos del programa para instalar.
echo Ejecute primero "Publicar_Y_Compilar.bat" o coloque los archivos en la carpeta publish.
echo.
pause
exit /b 1

:ORIGEN_OK
echo [1/5] Creando directorios permanentes en ProgramData...
if not exist "%DATA_DIR%" mkdir "%DATA_DIR%"
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"
if not exist "%IMAGES_DIR%" mkdir "%IMAGES_DIR%"

echo [2/5] Configurando permisos de lectura y escritura para usuarios de Windows...
icacls "%DATA_BASE%" /grant *S-1-5-32-545:(OI)(CI)M /T /Q >nul 2>&1
icacls "%DATA_BASE%" /grant "Usuarios":(OI)(CI)M /T /Q >nul 2>&1
icacls "%DATA_BASE%" /grant "Users":(OI)(CI)M /T /Q >nul 2>&1

echo [3/5] Verificando base de datos SQLite...
if exist "%DB_FILE%" goto :BD_EXISTE
echo       - Primera instalacion: la base de datos se inicializara automaticamente
echo         con los usuarios base y configuracion al iniciar el sistema.
goto :COPIAR_ARCHIVOS

:BD_EXISTE
echo       - Base de datos existente encontrada en: %DB_FILE%
echo       - SE CONSERVARAN TODOS LOS DATOS PREVIOS INTACTOS.

:COPIAR_ARCHIVOS
echo [4/5] Instalando archivos del programa en "%INSTALL_DIR%"...
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
robocopy "%SOURCE_DIR%" "%INSTALL_DIR%" /E /IS /IT /NP /NJH /NJS >nul

echo [5/5] Creando accesos directos en el Escritorio y Menu Inicio...
powershell -NoProfile -Command "$ws = New-Object -ComObject WScript.Shell; $desk = [Environment]::GetFolderPath('Desktop'); $s1 = $ws.CreateShortcut((Join-Path $desk 'DC BOUTIQUE.lnk')); $s1.TargetPath = 'C:\Program Files\DC_BOUTIQUE\DC_BOUTIQUEAPP.exe'; $s1.WorkingDirectory = 'C:\Program Files\DC_BOUTIQUE'; $s1.IconLocation = 'C:\Program Files\DC_BOUTIQUE\wwwroot\favicon.ico'; $s1.Description = 'DC Boutique - Punto de Venta'; $s1.Save(); $progs = [Environment]::GetFolderPath('CommonPrograms'); $s2 = $ws.CreateShortcut((Join-Path $progs 'DC BOUTIQUE.lnk')); $s2.TargetPath = 'C:\Program Files\DC_BOUTIQUE\DC_BOUTIQUEAPP.exe'; $s2.WorkingDirectory = 'C:\Program Files\DC_BOUTIQUE'; $s2.IconLocation = 'C:\Program Files\DC_BOUTIQUE\wwwroot\favicon.ico'; $s2.Description = 'DC Boutique - Punto de Venta'; $s2.Save()"

echo.
echo =========================================================================
echo                  INSTALACION COMPLETADA CON EXITO!
echo.
echo   * Programa:       %INSTALL_DIR%
echo   * Base de Datos:  %DB_FILE%
echo   * Respaldos:      %BACKUP_DIR%
echo   * Acceso Directo: Creado en el Escritorio
echo.
echo   El sistema es 100%% autonomo: no requiere SQL Server ni internet.
echo =========================================================================
echo.

set /p RESP="Desea iniciar DC_BOUTIQUE ahora mismo? (S/N): "
if /i "%RESP%"=="S" start "" "%INSTALL_DIR%\DC_BOUTIQUEAPP.exe"

exit /b 0
