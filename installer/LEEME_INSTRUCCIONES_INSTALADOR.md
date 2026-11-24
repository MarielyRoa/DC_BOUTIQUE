# DC_BOUTIQUE - Guía de Distribución e Instalación en PCs Terceras

Este paquete permite instalar y ejecutar **DC_BOUTIQUE** en cualquier computadora Windows de terceros sin requerir SQL Server, SSMS, Visual Studio, ni conexión a Internet.

---

## 1. Arquitectura de Almacenamiento Autónomo

| Componente | Ubicación Física Dinámica | Propósito |
| :--- | :--- | :--- |
| **Base de Datos SQLite** | `C:\ProgramData\DC_BOUTIQUE\Data\DC_BOUTIQUE.db` | Almacenamiento local persistente resuelto vía `Environment.SpecialFolder.CommonApplicationData`. |
| **Carpeta de Respaldos** | `C:\ProgramData\DC_BOUTIQUE\Backups\` | Copias de seguridad automáticas y manuales independientes. |
| **Archivos del Programa** | `C:\Program Files\DC_BOUTIQUE\` | Binarios ejecutables y vistas del sistema. |
| **Acceso Directo** | Escritorio y Menú Inicio de Windows | Acceso de 1 solo clic para el propietario o cajera. |

---

## 2. Requisitos Cumplidos

1. **Ruta Dinámica:** Resuelta mediante `Environment.SpecialFolder.CommonApplicationData` en C# (`DatabasePathHelper.cs`). No existen rutas fijas en el código.
2. **Permisos de Lectura/Escritura:** Configurados automáticamente con `icacls` para todos los usuarios estándar de Windows sin bloquearse por restricciones de `Program Files`.
3. **Persistencia Garantizada:** Al reinstalar o actualizar el programa, el archivo `DC_BOUTIQUE.db` se conserva intacto.
4. **Cero Dependencias de Servidores:** SQLite embebido (`Microsoft.EntityFrameworkCore.Sqlite`). No requiere instalar Microsoft SQL Server, servicios en segundo plano ni puertos abiertos.
5. **Autocreación y Siembra Segura:**
   - Si la base de datos no existe: Se crea automáticamente con las tablas y usuarios base:
     - **Administrador:** `admin` / PIN/Clave: `Admin123$`
     - **Cajera:** `caja01` / PIN/Clave: `Caja1234$`
     - **Cliente General:** `Consumidor Final`
   - Si la base de datos ya existe: Se respetan todas las ventas, prendas, turnos y clientes existentes.

---

## 3. ¿Cómo Crear el Instalador para Distribuir?

### Opción A (Recomendada con Inno Setup):
1. Ejecute el script `Publicar_Y_Compilar.bat`.
2. Si tiene instalado **Inno Setup 6**, generará automáticamente el ejecutable:
   `installer\output\DC_BOUTIQUE_Instalador_v1.0.exe`
3. Copie ese archivo `.exe` en una memoria USB o descárguelo en la PC cliente e instálelo normalmente.

### Opción B (Instalador Nativo Directo sin Inno Setup):
1. Ejecute `Publicar_Y_Compilar.bat`.
2. Copie la carpeta `installer\` a la PC del cliente.
3. En la PC cliente, haga doble clic en:
   `Instalar_DC_BOUTIQUE.bat`
4. ¡Listo! Se creará el acceso directo en el escritorio y el sistema estará 100% operativo.
