# Changelog

## 1.0.0 — 2026-10-20

Primera versión completa.

**Motor**
- Pipeline escanear/limpiar con `PathSafetyValidator` y `CleanupGate` como única vía de borrado.
- Exclusiones por prefijo y comodines, historial JSON Lines local.
- Limpieza por lotes de 512 candidatos para no inflar la memoria.

**Limpiadores**
- Temporales del usuario y de Windows, caché de miniaturas.
- Informes de errores de Windows (WER) y volcados de fallos.
- Cachés de Chrome, Edge, Brave, Vivaldi y Firefox mediante lista blanca de segmentos de ruta.
- Papelera de reciclaje vía API del shell, nunca preseleccionada.
- Cachés dentro de WSL: apt, journald, pip, npm y uv por distro.

**Aplicación**
- Shell WPF sobre .NET 10 con MVVM, simulacro, vista previa de archivos y confirmación.
- Selección masiva (todo/nada) y refresco automático de tamaños tras limpiar.
- Diagnóstico de arranque en `%LOCALAPPDATA%\Holgura\app.log`.

**Calidad**
- 27 tests xUnit en verde.
- Verificado en máquina real: 985 archivos / 805 MB escaneados en 3,5 s; 1.002 candidatos simulados sin errores.
