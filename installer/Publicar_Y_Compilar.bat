@echo off
chcp 65001 > nul
echo =========================================================================
echo    DC_BOUTIQUE - GENERADOR DE PAQUETE DE INSTALACIÓN STANDALONE
echo =========================================================================
echo.

cd /d "%~dp0.."

echo [1/3] Limpiando carpetas de publicación previa...
if exist "installer\publish" rmdir /s /q "installer\publish"
if exist "installer\output" rmdir /s /q "installer\output"
mkdir "installer\publish"
mkdir "installer\output"

echo.
echo [2/3] Publicando aplicación .NET 10 (Self-Contained win-x64)...
echo       (No requerirá .NET SDK ni Visual Studio en las PCs clientes)
dotnet publish DC_BOUTIQUE.WEBAPP\DC_BOUTIQUEAPP.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o "installer\publish"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] La publicación de la aplicación falló. Verifique los errores anteriores.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [3/3] Buscando compilador de Inno Setup (ISCC.exe)...
set "ISCC_PATH="
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"
if exist "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" set "ISCC_PATH=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"

if defined ISCC_PATH (
    echo Compilando instalador ejecutable con Inno Setup...
    "%ISCC_PATH%" "installer\DC_BOUTIQUE_Setup.iss"
    if %ERRORLEVEL% EQU 0 (
        echo.
        echo =========================================================================
        echo  ¡INSTALADOR CREADO EXITOSAMENTE!
        echo  Ubicación: installer\output\DC_BOUTIQUE_Instalador_v1.0.exe
        echo =========================================================================
    )
) else (
    echo.
    echo [INFO] Inno Setup no está instalado en la ruta por defecto.
    echo        Los archivos autónomos están listos en: installer\publish
    echo        También puede usar directamente: installer\Instalar_DC_BOUTIQUE.bat
)

echo.
pause
