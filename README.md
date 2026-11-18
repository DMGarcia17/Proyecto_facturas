# Proyecto_facturas

Sistema de facturación de escritorio (Windows Forms, .NET Framework 4.8) con base de datos MySQL.
Práctica de la clase de Base de Datos: login, formulario MDI con menú desplegable, CRUD de cada tabla,
facturas con detalle automático y reporte de ventas en Excel y PDF.

## Funcionalidades

- **Inicio de sesión** (sin opción de registro). Contraseñas con PBKDF2 + sal; bloqueo tras 5 intentos fallidos.
- **Formulario MDI** con menú desplegable: Productos, Clientes, Facturas, Usuarios, Ventana y Sesión.
- **CRUD** (Nuevo, Consultar, Editar, Eliminar) de Productos, Clientes y Usuarios.
- **Facturas**: nueva factura (cliente + productos), consultar con su detalle y eliminar (devuelve las existencias).
  Cada producto agregado genera un registro en `Detallefactura`. Todo se guarda en una transacción;
  los precios y totales se calculan en el servidor y se descuenta el stock.
- **Reporte de ventas** por rango de fechas y cliente, con exportación a **Excel** y **PDF**.
- **Validaciones** centralizadas en `Utilidades/Validador.cs` (ninguna pantalla define reglas propias).

Atajos: `Ctrl+N` nueva factura · `Ctrl+R` reporte de ventas · `Ctrl+L` cerrar sesión.

## Configuración

1. Copia `.env.example` como `.env` y completa tus datos (el `.env` no se sube a git):

   ```
   DB_HOST=...      DB_PORT=...     DB_NAME=facturacion
   DB_USER=...      DB_PASSWORD=... DB_SSL_MODE=Required
   IVA_PORCENTAJE=13
   ADMIN_USER=admin ADMIN_PASSWORD=...  ADMIN_FULLNAME=...
   ```

2. Compila y ejecuta:

   ```
   dotnet build
   dotnet run
   ```

### Carga inicial de la base de datos

Al iniciar, la aplicación ejecuta el cargador (`Datos/InicializadorBD.cs`):

- crea la base `DB_NAME` si no existe,
- crea las tablas definidas en `sql/01_schema.sql`,
- si no hay usuarios, crea el administrador indicado en `ADMIN_USER` / `ADMIN_PASSWORD`,
- si las tablas están vacías, carga algunos productos y clientes de ejemplo.

Es idempotente: ejecutarlo varias veces no duplica ni borra datos. Para empezar de cero, apunta
`DB_NAME` a una base nueva o elimina la actual.

## Base de datos

Estructura entregada por el docente: `Usuario`, `Producto`, `Cliente`, `Factura`, `Detallefactura`.
`Factura.Cliente` guarda el nombre del cliente (la estructura no usa llave foránea).

## Estructura del proyecto

```
Configuracion/   Lector del .env
Datos/           Conexión, cargador de BD y acceso a datos
Formularios/     Login, MDI, CRUD base y pantallas de facturas / reporte
Utilidades/      Estilo, Validador, Seguridad, exportadores Excel y PDF
sql/             Esquema de la base de datos
```
