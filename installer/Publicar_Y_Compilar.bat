@echo off
title Generador de Instalador - DC_BOUTIQUE
echo =========================================================================
echo    DC_BOUTIQUE - GENERADOR DE PAQUETE DE INSTALACION STANDALONE
echo =========================================================================
echo.

cd /d "%~dp0.."

echo [1/3] Limpiando carpetas de publicacion previa...
if exist "installer\publish" rmdir /s /q "installer\publish"
if exist "installer\output" rmdir /s /q "installer\output"
mkdir "installer\publish"
mkdir "installer\output"

echo.
echo [2/3] Publicando aplicacion .NET 10 (Self-Contained win-x64)...
echo       (No requerira .NET SDK ni Visual Studio en las PCs clientes)
dotnet publish DC_BOUTIQUE.WEBAPP\DC_BOUTIQUEAPP.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o "installer\publish"
if errorlevel 1 goto :ERROR_PUBLISH

echo.
echo [3/3] Buscando compilador de Inno Setup (ISCC.exe)...
set "ISCC_PATH="
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"
if exist "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" set "ISCC_PATH=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"

if not defined ISCC_PATH goto :SIN_INNO

echo Compilando instalador ejecutable con Inno Setup...
"%ISCC_PATH%" "installer\DC_BOUTIQUE_Setup.iss"
if errorlevel 1 goto :ERROR_INNO

echo.
echo =========================================================================
echo  INSTALADOR CREADO EXITOSAMENTE!
echo  Ubicacion: %CD%\installer\output\DC_BOUTIQUE_Instalador_v1.0.exe
echo =========================================================================
goto :FIN

:ERROR_PUBLISH
echo.
echo [ERROR] La publicacion de la aplicacion fallo. Verifique los errores anteriores.
pause
exit /b 1

:ERROR_INNO
echo.
echo [ERROR] Inno Setup reporto un error durante la compilacion.
pause
exit /b 1

:SIN_INNO
echo.
echo [INFO] Inno Setup no esta instalado en la ruta por defecto.
echo        Los archivos autonomos estan listos en: installer\publish
echo        Tambien puede usar directamente: installer\Instalar_DC_BOUTIQUE.bat

:FIN
echo.
pause
