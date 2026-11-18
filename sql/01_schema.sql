-- Esquema del sistema de facturacion (MySQL 8)
-- Las tablas y columnas siguen la estructura entregada por el docente.
-- Es idempotente: se puede ejecutar varias veces sin perder datos.

CREATE TABLE IF NOT EXISTS Usuario (
  IdUsuario      INT AUTO_INCREMENT PRIMARY KEY,
  Nombre         VARCHAR(50)  NOT NULL,
  NombreCompleto VARCHAR(150) NOT NULL,
  PasswordHash   VARCHAR(255) NOT NULL,
  PasswordSalt   VARCHAR(255) NOT NULL,
  FechaRegistro  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT uq_usuario_nombre UNIQUE (Nombre)
);

CREATE TABLE IF NOT EXISTS Producto (
  Idproducto  INT AUTO_INCREMENT PRIMARY KEY,
  Descripcion VARCHAR(100) NOT NULL,
  Cantidad    INT          NOT NULL DEFAULT 0,
  Precio      DOUBLE       NOT NULL,
  CONSTRAINT uq_producto_descripcion UNIQUE (Descripcion),
  CONSTRAINT ck_producto_cantidad CHECK (Cantidad >= 0),
  CONSTRAINT ck_producto_precio CHECK (Precio > 0)
);

CREATE TABLE IF NOT EXISTS Cliente (
  Idcliente INT AUTO_INCREMENT PRIMARY KEY,
  Nombre    VARCHAR(100) NOT NULL,
  Telefono  VARCHAR(20)  NOT NULL,
  Email     VARCHAR(150) NULL,
  Direccion VARCHAR(255) NULL,
  CONSTRAINT uq_cliente_nombre UNIQUE (Nombre),
  CONSTRAINT uq_cliente_email UNIQUE (Email)
);

-- Factura.Cliente guarda el nombre del cliente (la estructura del docente no usa llave foranea)
CREATE TABLE IF NOT EXISTS Factura (
  Idfactura INT AUTO_INCREMENT PRIMARY KEY,
  Cliente   VARCHAR(100) NOT NULL,
  Fecha     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  Subtotal  DOUBLE       NOT NULL,
  IVA       DOUBLE       NOT NULL,
  Total     DOUBLE       NOT NULL,
  INDEX ix_factura_fecha (Fecha),
  CONSTRAINT ck_factura_totales CHECK (Subtotal >= 0 AND IVA >= 0 AND Total >= 0)
);

CREATE TABLE IF NOT EXISTS Detallefactura (
  Iddetalle  INT AUTO_INCREMENT PRIMARY KEY,
  idfactura  INT    NOT NULL,
  idproducto INT    NOT NULL,
  cantidad   INT    NOT NULL,
  Precio     DOUBLE NOT NULL,
  Total      DOUBLE NOT NULL,
  CONSTRAINT fk_detalle_factura FOREIGN KEY (idfactura) REFERENCES Factura (Idfactura) ON DELETE CASCADE,
  CONSTRAINT fk_detalle_producto FOREIGN KEY (idproducto) REFERENCES Producto (Idproducto),
  CONSTRAINT ck_detalle_cantidad CHECK (cantidad > 0),
  CONSTRAINT ck_detalle_valores CHECK (Precio > 0 AND Total >= 0)
);
