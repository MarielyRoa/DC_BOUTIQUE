; =========================================================================
; INNO SETUP SCRIPT PARA INSTALADOR PROFESIONAL DE WINDOWS: DC_BOUTIQUE
; Totalmente Autónomo (Offline, SQLite en C:\ProgramData, Sin SQL Server)
; =========================================================================

#define MyAppName "DC_BOUTIQUE"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Dahiana Corporan Boutique"
#define MyAppExeName "DC_BOUTIQUEAPP.exe"
#define MyAppAssocName MyAppName + " File"

[Setup]
AppId={{9B7E31F4-4286-4E27-8E5A-DCBOUTIQUE01}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=output
OutputBaseFilename=DC_BOUTIQUE_Instalador_v1.0
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\wwwroot\favicon.ico

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Dirs]
; Crear carpetas en C:\ProgramData\DC_BOUTIQUE con permisos completos para lectura y escritura de usuarios
Name: "{commonappdata}\{#MyAppName}"; Permissions: users-full
Name: "{commonappdata}\{#MyAppName}\Data"; Permissions: users-full
Name: "{commonappdata}\{#MyAppName}\Data\Keys"; Permissions: users-full
Name: "{commonappdata}\{#MyAppName}\Backups"; Permissions: users-full
Name: "{commonappdata}\{#MyAppName}\Updates"; Permissions: users-full
Name: "{commonappdata}\{#MyAppName}\Images"; Permissions: users-full
Name: "{commonappdata}\{#MyAppName}\Images\productos"; Permissions: users-full
Name: "{commonappdata}\{#MyAppName}\AppWindowProfile"; Permissions: users-full

[Files]
; Archivos publicados de la aplicación (.NET self-contained win-x64)
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\wwwroot\favicon.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\wwwroot\favicon.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Eliminar archivos de la aplicación instalada pero NUNCA borrar la base de datos ni los respaldos en ProgramData
Type: filesandordirs; Name: "{app}"

[Code]
// El código garantiza que C:\ProgramData\DC_BOUTIQUE\Data\DC_BOUTIQUE.db se preserve intacto en actualizaciones o desinstalaciones
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Garantizar que la carpeta de datos y respaldos tenga permisos de modificación
    // para cualquier usuario estándar de Windows sin UAC
  end;
end;
