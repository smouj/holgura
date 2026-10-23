# Guía de uso

## Inicio: espacio y archivos

1. Al abrir Inicio, Holgura analiza una vez las ubicaciones temporales y cachés incluidas en sus módulos. Puedes repetirlo con **Analizar equipo** o cancelar el análisis; escanear solo lee y calcula, no borra.
2. La tarjeta de unidad muestra espacio libre y uso actual de las unidades fijas disponibles. La tarjeta de recuperación suma los candidatos de las zonas analizadas; no representa archivos de todo el disco.
3. Elige una zona en la columna izquierda para ver la lista de archivos, ruta, tamaño y fecha de modificación.
4. Marca varios archivos concretos o marca la zona para seleccionar todos sus candidatos. **Seleccionar zonas seguras** reemplaza la selección actual y marca solo grupos clasificados como seguros; las zonas **Revisar** requieren selección manual. Si una zona contiene más de 2.000 candidatos, el panel muestra los 2.000 mayores para selección individual y avisa que el grupo completo incluye los demás.
5. Pulsa **Simular** para ver cuántos archivos se procesarían y el tamaño estimado. La simulación no borra nada.
6. Pulsa **Eliminar selección** y confirma. Holgura comprueba permisos, exclusiones, rutas protegidas, puntos de reanálisis y cambios en los archivos. Los archivos que ya no coinciden con el análisis se conservan y se reportan.

No se selecciona nada automáticamente. Los elementos bloqueados por exclusiones o por Project Infest no se pueden marcar. Al terminar, la app vuelve a analizar y actualiza el espacio disponible.

## Aplicaciones y juegos

La pestaña **Aplicaciones** muestra programas que Windows registra en las claves de desinstalación del equipo o del usuario. Puedes buscar por nombre, editor o versión y revisar la ubicación y el tamaño estimado cuando Windows los informa.

Para desinstalar, selecciona una entrada habilitada y confirma. Holgura abre el desinstalador oficial con su ventana normal; el usuario revisa y completa ese asistente. Holgura no borra manualmente la carpeta del programa. Las entradas de Project Infest quedan bloqueadas. Si una entrada requiere un intérprete o script, Holgura la deja inhabilitada y explica que debe gestionarse desde Configuración de Windows.

Las aplicaciones de Microsoft Store se administran con **Administrar apps de Microsoft Store**, que abre Configuración de Windows. Los componentes de sistema ocultos por Windows no aparecen en la lista.

## Historial y exclusiones

**Historial** conserva los resultados de simulaciones y limpiezas en el equipo. **Exclusiones** permite guardar una ruta o patrón para que la puerta de seguridad lo respete en operaciones posteriores.

Los análisis y la lista de programas se leen localmente. Holgura no transmite nombres de archivo ni datos de instalación.
