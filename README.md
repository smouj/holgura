<p align="center">
  <img src="assets/logo.svg" width="104" height="104" alt="Holgura" />
</p>

<h1 align="center">Holgura</h1>

<p align="center">
  <strong>Espacio para respirar.</strong><br/>
  Limpiador para Windows y WSL que te enseña cada archivo antes de borrarlo.
</p>

<p align="center">
  <a href="https://github.com/smouj/holgura/actions/workflows/ci.yml"><img alt="CI" src="https://img.shields.io/github/actions/workflow/status/smouj/holgura/ci.yml?branch=main&label=build&logo=github"></a>
  <a href="https://github.com/smouj/holgura/releases"><img alt="Release" src="https://img.shields.io/github/v/release/smouj/holgura?label=release"></a>
  <img alt="License" src="https://img.shields.io/github/license/smouj/holgura">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10-512BD4">
  <img alt="Windows" src="https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4">
  <img alt="Privacy" src="https://img.shields.io/badge/telemetry-none-2ea44f">
</p>

---

## Por qué otro limpiador

La mayoría pide confianza y devuelve una barra de progreso. Holgura devuelve la lista de archivos:
cada categoría, su tamaño, su ruta y el motivo por el que puede irse.

- Todo borrado pasa por una única puerta que revalida raíz, exclusiones y archivo.
- Las raíces críticas (unidades, `Windows`, `Program Files`, tu perfil, Documentos, Escritorio) nunca pueden ser raíz de borrado.
- Los junctions y puntos de reanálisis no se siguen jamás.
- Un archivo bloqueado se omite y se reporta; no es fatal.
- Nada se borra hasta que pulsas el botón, y **Simular limpieza** enseña el plan antes.

## Qué limpia

| Módulo | Nivel | Notas |
|---|---|---|
| Temporales del usuario (`%TEMP%`) | Seguro | Se regeneran |
| Temporales de Windows (`\Windows\Temp`) | Revisar | Parte requiere administrador; el resto se omite y se reporta |
| Caché de miniaturas | Seguro | Explorer la reconstruye |
| Informes de errores (WER) | Seguro / Revisar | Usuario y cola del sistema |
| Volcados de fallos | Seguro / Revisar | `CrashDumps` y `Minidump` |
| Cachés de navegador | Seguro | Chrome, Edge, Brave, Vivaldi y Firefox: solo caché |
| Papelera de reciclaje | Revisar | Nunca preseleccionada; se vacía por la API del shell |
| Cachés dentro de WSL | Seguro / Revisar | apt, journald, pip, npm, uv por distro |

### Lo que nunca toca

Cookies, historial, contraseñas, sesiones, extensiones, marcadores, descargas, documentos,
proyectos, `Program Files` y cualquier ruta que añadas a la lista de exclusiones.

## Modelo de seguridad

```
ESCANEAR -> VALIDAR -> SELECCIONAR -> VALIDACIÓN PREVIA -> BORRAR -> VERIFICAR -> REPORTAR
```

1. Un módulo solo declara un directorio raíz. Nunca recibe permiso de borrado.
2. `PathSafetyValidator` rechaza raíces de unidad, carpetas protegidas y cualquier raíz que las contenga.
3. `CleanupGate` revalida contención, exclusiones, puntos de reanálisis y existencia justo antes de borrar.
4. Un archivo más nuevo que el escaneo (más 5 minutos de margen) se marca `ChangedSinceScan`.
5. Un fallo en un archivo se registra y la operación continúa.
6. Los candidatos se procesan en lotes de 512 para no inflar la memoria en equipos ya justos.

La papelera y WSL son `DeletionScope.External`: el motor les entrega una puerta que **rechaza**
cualquier borrado de archivo, y solo pueden ejecutar su propia operación acotada.

## WSL

Windows no ve dentro de `ext4.vhdx`, así que WSL va por su propio camino:

- `IWslBridge` ejecuta `wsl.exe` con lista de argumentos, nunca una cadena de shell compuesta.
- La medición es un único script `bash -lc` que informa bytes y número de archivos por caché.
- Una medición fallida **es un error**, no un resultado vacío: la app nunca dice "no hay nada" cuando no pudo mirar.
- La limpieza solo ejecuta recetas fijas de `WslRecipes`; lo que no está en la lista se resuelve a `null` y falla cerrado.
- Las cachés de usuario se limpian directamente; `apt` y `journald` exigen `sudo` sin contraseña y se reportan cuando no está.

Liberar espacio dentro de una distro no encoge el VHDX. Compactarlo requiere `wsl --shutdown` y
`Optimize-VHD` con permisos de administrador, y la app lo dice en vez de fingir que lo ha hecho.

## Instalación

Portable: sin instalador, sin servicio, sin tarea programada.

1. Descarga `Holgura-win-x64.zip` desde [Releases](https://github.com/smouj/holgura/releases).
2. Descomprime donde quieras y ejecuta `Holgura.App.exe`.

Diagnóstico en `%LOCALAPPDATA%\Holgura\app.log`.

## Compilar

Requiere el SDK de .NET 10 en Windows.

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet publish src/Holgura.App/Holgura.App.csproj -c Release -r win-x64 --self-contained true
```

## Arquitectura

```
src/
  Holgura.Contracts/          Interfaces, resultados y niveles de seguridad. Sin dependencias.
  Holgura.Core/               PathSafetyValidator, exclusiones, historial, formato.
  Holgura.Engine/             Pipeline de escaneo/limpieza, CleanupGate, módulos.
  Holgura.Platform.Windows/   Sistema de ficheros, puente WSL, papelera, rutas.
  Holgura.App/                Shell de escritorio (WPF, MVVM).
tests/
  Holgura.Core.Tests/
  Holgura.Engine.Tests/
tools/
  SmokeTest/                  Escaneo y simulación sin interfaz contra la máquina real.
```

El motor no sabe nada de la interfaz, y la interfaz no sabe nada de las reglas de borrado.

### Nota sobre el shell

La primera iteración apuntó a WinUI 3. Compilarlo exige las tareas *Appx* de MSBuild que solo
vienen con el workload "Windows application development" de Visual Studio. La capa XAML usa WPF
sobre .NET 10, que compila solo con el SDK. Todo lo que hay debajo de `Holgura.App` es idéntico:
cambiar de shell es un cambio de un solo proyecto.

## Verificado en hardware real

```
== SCAN ==
items=3  files=985  bytes=805 MB  (3.5 s)  warnings=0
  514.0 MB | Review | C:\WINDOWS\Temp
  153.4 MB | Safe   | ...\Windows\Explorer (miniaturas)
  100.2 MB | Safe   | ...\AppData\Local\Temp

== DRY RUN ==
wouldDelete=1,002  skipped=0  errors=0  (0.7 s)

== SAFETY ==
rechaza C:\, C:\Windows, C:\Program Files, C:\Users\<usuario>, Documentos
```

27 tests unitarios cubren el validador, las exclusiones y el motor: archivos bloqueados, puntos de
reanálisis, raíces protegidas, simulaciones, cancelación y filtros de antigüedad.

## Hoja de ruta

- **v1.0** motor, siete limpiadores, WSL, simulacro, historial, tests y CI ✅
- **v1.1** analizador de almacenamiento por carpetas
- **v1.2** cachés de desarrollo (NuGet, npm, pnpm, Yarn, pip, Gradle, Cargo, VS Code)
- **v1.3** modo administrador opt-in para `Windows\Temp` y `Minidump`
- **v2.0** shell WinUI 3 cuando el toolchain esté disponible

## Contribuir

Lee [CONTRIBUTING.md](CONTRIBUTING.md). Seguridad: [SECURITY.md](SECURITY.md).

## Privacidad

Todo ocurre en local. Sin cuenta, sin servidor, sin analítica, sin comprobación de actualizaciones
que reporte uso. Tus nombres de archivo no salen de la máquina.

## Licencia

MIT — ver [LICENSE](LICENSE).
