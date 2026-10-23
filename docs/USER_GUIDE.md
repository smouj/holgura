# Guía de uso

## Tamaño de ventana

Al abrirse, Holgura se centra y limita su tamaño al área útil del monitor actual, teniendo en
cuenta la barra de tareas y la escala de pantalla de Windows. Puedes cambiar su tamaño; en ventanas
estrechas, la barra lateral muestra solo los iconos y mantiene sus ayudas emergentes.

## Inicio: espacio y archivos

1. Al abrir Inicio, Holgura analiza una vez las ubicaciones temporales y cachés incluidas en sus módulos. Puedes repetirlo con **Analizar equipo** o cancelar el análisis; escanear solo lee y calcula, no borra.
2. La tarjeta de unidad muestra espacio libre y uso actual de las unidades fijas disponibles. La tarjeta de recuperación suma los candidatos de las zonas analizadas; no representa archivos de todo el disco.
3. Elige una zona en la columna izquierda para ver la lista de archivos, ruta, tamaño y fecha de modificación.
4. Usa el menú de orden para listar los archivos por tamaño mayor a menor, menor a mayor o por nombre. Las zonas se ordenan por el espacio que ocupan, de mayor a menor.
5. Marca varios archivos concretos o marca la zona para seleccionar todos sus candidatos. **Solo seguras** reemplaza la selección actual y marca solo grupos clasificados como seguros; las zonas **Revisar** requieren selección manual. Si una zona contiene más de 2.000 candidatos, el panel muestra los 2.000 mayores para selección individual y avisa que el grupo completo incluye los demás.
6. Pulsa **Simular** para ver cuántos archivos se procesarían y el tamaño estimado. La simulación no borra nada.
7. Pulsa **Eliminar selección** y confirma. Holgura comprueba permisos, exclusiones, rutas protegidas, puntos de reanálisis y cambios en los archivos. Los archivos que ya no coinciden con el análisis se conservan y se reportan.

No se selecciona nada automáticamente. Los elementos bloqueados por exclusiones o por Project Infest no se pueden marcar. Al terminar, la app vuelve a analizar y actualiza el espacio disponible.

## Aplicaciones y juegos

La pestaña **Aplicaciones** muestra programas que Windows registra en las claves de desinstalación del equipo o del usuario. Puedes buscar por nombre, editor o versión, filtrar por unidad de instalación, ordenar por tamaño mayor a menor, menor a mayor o nombre, y revisar la ubicación y el tamaño estimado cuando Windows los informa. La unidad se calcula a partir de la ruta de instalación que registra Windows; si no la informa, la app indica **Unidad no informada** en vez de adivinar. Al cambiar de unidad, se quita la selección de las aplicaciones que quedan ocultas. Las entradas sin tamaño quedan al final de ambos órdenes por tamaño.

Para desinstalar, selecciona una o varias entradas habilitadas y confirma. Holgura abre sus desinstaladores oficiales de uno en uno; al terminar cada asistente, confirma si quieres iniciar el siguiente. Puedes detener la secuencia en cualquier momento. Holgura no borra manualmente las carpetas de los programas. Las entradas de Project Infest quedan bloqueadas. Si una entrada requiere un intérprete o script, Holgura la deja inhabilitada y explica que debe gestionarse desde Configuración de Windows.

Las aplicaciones de Microsoft Store se administran con **Administrar apps de Microsoft Store**, que abre Configuración de Windows. Los componentes de sistema ocultos por Windows no aparecen en la lista.

## Instalar

El instalador de una release se instala para el usuario actual, crea accesos directos y puede iniciar
Holgura al terminar. La aplicación es autocontenida; no requiere una instalación separada de .NET.
También puedes usar el ZIP portable sin instalar.

## Historial y exclusiones

**Historial** conserva los resultados de simulaciones y limpiezas en el equipo. **Exclusiones** permite guardar una ruta o patrón para que la puerta de seguridad lo respete en operaciones posteriores.

Los análisis y la lista de programas se leen localmente. Holgura no transmite nombres de archivo ni datos de instalación.

## Capturas para documentación

Desde la raíz del repositorio, ejecuta `dotnet run --project Holgura.App/Holgura.App.csproj -c Debug -- --capture-readme-screenshots` para regenerar `docs/screenshots/`. El modo de captura usa datos ficticios y deshabilita análisis, limpieza, actualización de la lista y desinstalación; solo está disponible en compilaciones Debug.
