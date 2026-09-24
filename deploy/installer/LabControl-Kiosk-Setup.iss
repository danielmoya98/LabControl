; ==============================================================================
; LabControl Kiosk - Script de Instalación Oficial para Terminales de Laboratorio
; Universidad del Valle (Univalle)
; Compatible con Inno Setup 6.x
; ==============================================================================

#define MyAppId "8B84976A-4DC2-4FD8-BD48-F517227491C3"
#define MyAppName "LabControl Kiosk"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Universidad del Valle"
#define MyAppURL "https://www.univalle.edu"
#define MyAppExeName "LabControl.Client.Kiosk.exe"
#define DefaultApiUrl "http://192.168.50.132:5256"
#define DefaultAulaId "1"
#define MasterTechnicianKey "AdminLab@2026"

[Setup]
; Identificador único de aplicación para evitar instalaciones duplicadas
AppId={{{#MyAppId}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Ubicación por defecto en Program Files (Permite al técnico elegir otra carpeta)
DefaultDirName={autopf}\LabControl\Kiosk
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Configuración de salida del ejecutable Setup
OutputDir=..\..\dist\installer
OutputBaseFilename=LabControl_Kiosk_Setup_v1.0
Compression=lzma2/ultra64
SolidCompression=yes

; Apariencia moderna idéntica a los estándares de Windows
WizardStyle=modern
WizardSizePercent=100
DisableDirPage=no

; Requerir privilegios de Administrador para proteger archivos del sistema
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=commandline

; Arquitectura de 64 bits nativa
ArchitecturesInstallIn64BitMode=x64compatible

; Cierre forzado de instancias previas antes de actualizar
CloseApplications=force
CloseApplicationsFilter=*.exe

; Desinstalador seguro
UninstallDisplayName={#MyAppName} - Terminal de Laboratorio
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Dirs]
; Restricción estricta de permisos NTFS en la carpeta de instalación:
; Administradores y SYSTEM tienen control total.
; Usuarios estándar (estudiantes) solo tienen permiso de lectura y ejecución.
Name: "{app}"; Permissions: users-readexec authusers-readexec admins-full system-full
; Carpeta de datos comunes para SQLite y configuración accesible para todos los usuarios:
Name: "{commonappdata}\LabControl"; Permissions: users-full authusers-full admins-full system-full

[Files]
; Binarios compilados autónomos (Self-Contained win-x64)
Source: "..\..\dist\LabControl-Kiosk-Client\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Permissions: users-readexec authusers-readexec admins-full system-full

[Registry]
; 1. Arranque automático estándar en Registro de Windows (Run)
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "LabControlKiosk"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue

; 2. Ocultar la aplicación de "Configuración -> Aplicaciones instaladas" y "Panel de Control -> Desinstalar programas"
; Al registrar SystemComponent=1 y NoRemove=1, Windows oculta completamente la aplicación de la interfaz de usuario.
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{{{#MyAppId}}_is1"; ValueType: dword; ValueName: "SystemComponent"; ValueData: 1; Flags: uninsdeletekeyifempty
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{{{#MyAppId}}_is1"; ValueType: dword; ValueName: "NoRemove"; ValueData: 1; Flags: uninsdeletekeyifempty

[Run]
; 2. Crear Tarea Programada en Task Scheduler (Arranque con máximos privilegios garantizados al encender/iniciar sesión)
Filename: "schtasks.exe"; Parameters: "/Create /TN ""LabControlKioskStartup"" /TR """"{app}\{#MyAppExeName}"""" /SC ONLOGON /RL HIGHEST /F"; Flags: runhidden

; 3. Endurecimiento de permisos NTFS (Anti-borrado y Anti-manipulación por estudiantes)
; Otorga solo lectura y ejecución al grupo Usuarios y deniega escritura/eliminación
Filename: "icacls.exe"; Parameters: """{app}"" /grant *S-1-5-32-545:(OI)(CI)RX /grant *S-1-5-32-544:(OI)(CI)F /grant *S-1-5-18:(OI)(CI)F /inheritance:r"; Flags: runhidden
; Permitir a los usuarios modificar el directorio de configuración cuando la PC se auto-registre
Filename: "icacls.exe"; Parameters: """{commonappdata}\LabControl"" /grant *S-1-5-32-545:(OI)(CI)M"; Flags: runhidden

; 4. Lanzar la aplicación inmediatamente al finalizar la instalación
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Matar proceso si está activo antes de desinstalar
Filename: "taskkill.exe"; Parameters: "/F /IM {#MyAppExeName}"; Flags: runhidden; RunOnceId: "KillKioskProcess"
Filename: "taskkill.exe"; Parameters: "/F /IM LabControl.Guardian.exe"; Flags: runhidden; RunOnceId: "KillGuardianProcess"

; Eliminar la Tarea Programada de arranque
Filename: "schtasks.exe"; Parameters: "/Delete /TN ""LabControlKioskStartup"" /F"; Flags: runhidden; RunOnceId: "DelScheduledTask"

; Restaurar política del Administrador de Tareas en caso de estar deshabilitado
Filename: "reg.exe"; Parameters: "delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\System"" /v DisableTaskMgr /f"; Flags: runhidden; RunOnceId: "DelTaskMgrPolicy"

[Code]
var
  ConfigPage: TInputQueryWizardPage;

// ==============================================================================
// 1. PÁGINA PERSONALIZADA DE CONFIGURACIÓN DEL SERVIDOR CENTRAL
// ==============================================================================
procedure InitializeWizard;
begin
  ConfigPage := CreateInputQueryPage(
    wpSelectDir,
    'Configuración de Servidor Central',
    'Parámetros de conexión al sistema central de Univalle',
    'Por favor verifique la dirección del servidor API central para este equipo:' + #13#10 +
    '(El aula se seleccionará de manera dinámica e interactiva de la base de datos al finalizar)'
  );

  ConfigPage.Add('URL del Servidor Central (API):', False);
  ConfigPage.Add('Clave Maestra de Desbloqueo Técnico:', True);

  // Valores predeterminados iniciales
  ConfigPage.Values[0] := ExpandConstant('{#DefaultApiUrl}');
  ConfigPage.Values[1] := ExpandConstant('{#MasterTechnicianKey}');
end;

// ==============================================================================
// 2. GENERACIÓN AUTOMÁTICA DEL ARCHIVO kiosk-config.json AL INSTALAR
// ==============================================================================
procedure CurStepChanged(CurStep: TSetupStep);
var
  ConfigJson: String;
  ConfigFilePath: String;
  ServerUrl: String;
  AulaIdStr: String;
  ClaveTec: String;
  Hostname: String;
begin
  if CurStep = ssPostInstall then
  begin
    // Leer parámetros ingresados o usar los pasados por línea de comandos
    ServerUrl := ExpandConstant('{param:APIURL|' + ConfigPage.Values[0] + '}');
    // AulaId opcional si se pasa por comando silencioso (ej: /AULAID=2), por defecto 0 para abrir selector interactivo
    AulaIdStr := ExpandConstant('{param:AULAID|0}');
    ClaveTec := ExpandConstant('{param:CLAVETECNICO|' + ConfigPage.Values[1] + '}');
    Hostname := ExpandConstant('{%COMPUTERNAME}');

    // Normalizar URL del servidor
    ServerUrl := Trim(ServerUrl);
    if ServerUrl = '' then
      ServerUrl := '{#DefaultApiUrl}';
    if (Pos('http://', Lowercase(ServerUrl)) = 0) and (Pos('https://', Lowercase(ServerUrl)) = 0) then
      ServerUrl := 'http://' + ServerUrl;
    if ServerUrl[Length(ServerUrl)] <> '/' then
      ServerUrl := ServerUrl + '/';

    // Normalizar ID del Aula
    AulaIdStr := Trim(AulaIdStr);
    if (AulaIdStr = '') or (StrToIntDef(AulaIdStr, -1) < 0) then
      AulaIdStr := '0';

    if Trim(ClaveTec) = '' then
      ClaveTec := '{#MasterTechnicianKey}';

    // Generar JSON de configuración del Kiosk (AulaId=0 para que al iniciar lance el selector de aulas dinámico)
    ConfigJson :=
      '{' + #13#10 +
      '  "ApiBaseUrl": "' + ServerUrl + '",' + #13#10 +
      '  "AulaId": ' + AulaIdStr + ',' + #13#10 +
      '  "AulaNombre": "",' + #13#10 +
      '  "ComputadoraId": 0,' + #13#10 +
      '  "Hostname": "' + Hostname + '",' + #13#10 +
      '  "MacAddress": "",' + #13#10 +
      '  "ClaveTecnico": "' + ClaveTec + '"' + #13#10 +
      '}';

    // Guardar exclusivamente en {commonappdata}\LabControl\kiosk-config.json (directorio de datos dinámicos)
    ForceDirectories(ExpandConstant('{commonappdata}\LabControl'));
    SaveStringToFile(ExpandConstant('{commonappdata}\LabControl\kiosk-config.json'), ConfigJson, False);

    // Asegurar que no quede un archivo de configuración residual con clave en texto plano en la carpeta de binarios {app}
    ConfigFilePath := ExpandConstant('{app}\kiosk-config.json');
    if FileExists(ConfigFilePath) then
      DeleteFile(ConfigFilePath);
  end;
end;

// ==============================================================================
// 3. SEGURIDAD ANTI-SABOTAJE: DESINSTALACIÓN PROTEGIDA CON CONTRASEÑA
// Evita que estudiantes curiosos ejecuten unins000.exe para quitar el programa.
// ==============================================================================
function InitializeUninstall(): Boolean;
var
  Form: TSetupForm;
  Lbl: TLabel;
  Edit: TPasswordEdit;
  BtnOk, BtnCancel: TNewButton;
  W, I: Integer;
begin
  // 1. Si se invoca con el parámetro /AUTHORIZED desde el menú técnico del Kiosk,
  // permitir la desinstalación directa sin solicitar la contraseña dos veces.
  for I := 1 to ParamCount do
  begin
    if (CompareText(ParamStr(I), '/AUTHORIZED') = 0) or
       (CompareText(ParamStr(I), '-AUTHORIZED') = 0) then
    begin
      Result := True;
      Exit;
    end;
  end;

  // 2. Si un usuario ejecuta directamente unins000.exe, exigir la clave maestra de soporte técnico
  Form := CreateCustomForm(ScaleX(380), ScaleY(160), False, True);
  try
    Form.Caption := 'Desinstalación Protegida de Laboratorio';

    Lbl := TLabel.Create(Form);
    Lbl.Parent := Form;
    Lbl.Left := ScaleX(16);
    Lbl.Top := ScaleY(14);
    Lbl.Width := ScaleX(348);
    Lbl.Caption := 'ATENCIÓN: Esta acción desprotegerá la terminal de laboratorio.' + #13#10 +
                   'Ingrese la Clave Maestra de Técnico Autorizado:';

    Edit := TPasswordEdit.Create(Form);
    Edit.Parent := Form;
    Edit.Left := ScaleX(16);
    Edit.Top := ScaleY(65);
    Edit.Width := ScaleX(348);
    Edit.Height := ScaleY(24);

    BtnOk := TNewButton.Create(Form);
    BtnOk.Parent := Form;
    BtnOk.Caption := 'Aceptar';
    BtnOk.Left := ScaleX(180);
    BtnOk.Top := ScaleY(105);
    BtnOk.Height := ScaleY(26);
    BtnOk.ModalResult := mrOk;
    BtnOk.Default := True;

    BtnCancel := TNewButton.Create(Form);
    BtnCancel.Parent := Form;
    BtnCancel.Caption := 'Cancelar';
    BtnCancel.Left := ScaleX(275);
    BtnCancel.Top := ScaleY(105);
    BtnCancel.Height := ScaleY(26);
    BtnCancel.ModalResult := mrCancel;
    BtnCancel.Cancel := True;

    W := Form.CalculateButtonWidth([BtnOk.Caption, BtnCancel.Caption]);
    BtnOk.Width := W;
    BtnCancel.Width := W;

    Form.ActiveControl := Edit;

    if Form.ShowModal() = mrOk then
    begin
      if Edit.Text = '{#MasterTechnicianKey}' then
      begin
        Result := True;
      end
      else
      begin
        MsgBox('ERROR DE AUTENTICACIÓN: Clave de técnico incorrecta.' + #13#10 +
               'La desinstalación ha sido bloqueada por seguridad.', mbCriticalError, MB_OK);
        Result := False;
      end;
    end
    else
    begin
      Result := False;
    end;
  finally
    Form.Free();
  end;
end;
