# Changelog

## En desarrollo — 2026-10-23

**Interfaz y análisis**
- Panel de almacenamiento con capacidad, espacio libre y usado por unidad fija.
- Indicadores con capacidad total y porcentaje usado; el resumen distingue candidatos detectados del espacio realmente libre en la unidad.
- La ventana se ajusta al área útil del monitor y su navegación se compacta al estrecharla.
- Inicio ejecuta un análisis de solo lectura al abrirse para mostrar el estado disponible desde el primer momento.
- Lista virtualizada de archivos por categoría, con rutas, tamaños, fechas y selección individual de varios archivos.
- Orden de archivos y programas por tamaño ascendente/descendente o por nombre; los elementos sin tamaño informado quedan al final.
- Vista previa limitada a los 2.000 archivos mayores por zona; el análisis conserva las cifras agregadas exactas.
- Simulación en pantalla, confirmación explícita y estado de operación; análisis y limpieza no bloquean la interfaz.
- Navegación e interacción con transiciones suaves y estados de carga.

**Aplicaciones**
- Inventario de programas y juegos registrados por Windows, con búsqueda, editor, versión, tamaño estimado y ruta.
- Desinstalación secuencial de una selección de aplicaciones mediante sus asistentes oficiales visibles; puedes detener la secuencia y Holgura no borra manualmente las carpetas de programas.
- Componentes del sistema ocultos, rutas de Project Infest bloqueadas y comandos que invoquen intérpretes o scripts deshabilitados.
- Acceso directo a Configuración de Windows para administrar aplicaciones de Microsoft Store.

**Seguridad y documentación**
- El logo azul/blanco se aplica en la ventana, la navegación y el icono de Windows con el recurso empaquetado coherente en tema claro y oscuro.
- Capturas del README generadas en un modo de demostración Debug, con datos ficticios y acciones peligrosas deshabilitadas.
- Instalador autocontenido por usuario configurado para releases, con accesos directos en Inicio y escritorio y apertura opcional al finalizar.
- La integración WSL normaliza los finales de línea y envía scripts fijos por stdin; corrige el error de análisis en Ubuntu.
- Project Infest se excluye del análisis y se vuelve a proteger en la puerta de borrado.
- Los archivos seleccionados individualmente se comparan con el tamaño y fecha del análisis y se conservan si cambiaron.
- Guía de usuario, arquitectura, compilación y alcance de la lista de aplicaciones actualizados.
- Verificación local: compilación Release sin advertencias, 36 pruebas en verde y escaneo/simulación sin borrados.

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
