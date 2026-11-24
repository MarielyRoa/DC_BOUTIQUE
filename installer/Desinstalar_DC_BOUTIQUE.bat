@echo off
title Desinstalador de DC_BOUTIQUE

echo =========================================================================
echo                 DESINSTALADOR DE DC_BOUTIQUE
echo =========================================================================
echo.

net session >nul 2>&1
if errorlevel 1 goto :PEDIR_ADMIN
goto :INICIAR_DESINSTALACION

:PEDIR_ADMIN
echo [SOLICITANDO PERMISOS] Se requieren privilegios de Administrador para desinstalar...
powershell -NoProfile -Command "Start-Process '%~f0' -Verb RunAs"
exit /b

:INICIAR_DESINSTALACION
set "INSTALL_DIR=C:\Program Files\DC_BOUTIQUE"
set "DATA_DIR=C:\ProgramData\DC_BOUTIQUE"

echo [1/3] Cerrando procesos de DC_BOUTIQUE si estan en ejecucion...
taskkill /f /im DC_BOUTIQUEAPP.exe >nul 2>&1

echo [2/3] Eliminando accesos directos...
powershell -NoProfile -Command "$desk = [Environment]::GetFolderPath('Desktop'); Remove-Item (Join-Path $desk 'DC BOUTIQUE.lnk') -Force -ErrorAction SilentlyContinue; $progs = [Environment]::GetFolderPath('CommonPrograms'); Remove-Item (Join-Path $progs 'DC BOUTIQUE.lnk') -Force -ErrorAction SilentlyContinue"

echo [3/3] Eliminando archivos de la aplicacion en "%INSTALL_DIR%"...
if exist "%INSTALL_DIR%" rmdir /s /q "%INSTALL_DIR%"

echo.
echo =========================================================================
echo La aplicacion ha sido desinstalada de Program Files.
echo.
echo Su base de datos y respaldos se encuentran seguros en:
echo %DATA_DIR%
echo.
set /p DELDATA="Desea ELIMINAR permanentemente la base de datos y respaldos? (Escriba BORRAR para confirmar, o presione Enter para conservarlos): "
if /i "%DELDATA%"=="BORRAR" goto :BORRAR_DATOS

echo La base de datos y respaldos fueron conservados.
goto :FIN

:BORRAR_DATOS
if exist "%DATA_DIR%" rmdir /s /q "%DATA_DIR%"
echo Base de datos y respaldos eliminados.

:FIN
echo =========================================================================
echo.
pause
