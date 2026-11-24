using DC_BOUTIQUE.Core.Application.DTOs.Caja;
using DC_BOUTIQUE.Core.Application.DTOs.Categorias;
using DC_BOUTIQUE.Core.Application.DTOs.Clientes;
using DC_BOUTIQUE.Core.Application.DTOs.Inventario;
using DC_BOUTIQUE.Core.Application.DTOs.Productos;
using DC_BOUTIQUE.Core.Application.DTOs.Usuarios;
using DC_BOUTIQUE.Core.Application.DTOs.Ventas;
using DC_BOUTIQUE.Core.Application.ViewModels.Caja;
using DC_BOUTIQUE.Core.Application.ViewModels.Categorias;
using DC_BOUTIQUE.Core.Application.ViewModels.Clientes;
using DC_BOUTIQUE.Core.Application.ViewModels.Inventario;
using DC_BOUTIQUE.Core.Application.ViewModels.Productos;
using DC_BOUTIQUE.Core.Application.ViewModels.Usuarios;
using DC_BOUTIQUE.Core.Application.ViewModels.Ventas;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using DC_BOUTIQUE.Core.Domain.Entities;

namespace DC_BOUTIQUE.Core.Application.Extensions
{
    public static class MappingExtensions
    {
        #region Usuarios
        public static UsuarioViewModel ToViewModel(this UsuarioDto dto)
        {
            return new UsuarioViewModel
            {
                Id = dto.Id,
                NombreCompleto = dto.NombreCompleto,
                NombreUsuario = dto.NombreUsuario,
                Cedula = dto.Cedula,
                Telefono = dto.Telefono,
                Rol = dto.Rol,
                PorcentajeComision = dto.PorcentajeComision,
                Activo = dto.Activo,
                FechaCreacion = dto.FechaCreacion
            };
        }

        public static SaveUsuarioDto ToDto(this SaveUsuarioViewModel vm)
        {
            return new SaveUsuarioDto
            {
                Id = vm.Id,
                NombreCompleto = vm.NombreCompleto,
                NombreUsuario = vm.NombreUsuario,
                Password = vm.Password,
                Cedula = vm.Cedula,
                Telefono = vm.Telefono,
                Rol = vm.Rol,
                PorcentajeComision = vm.PorcentajeComision,
                Activo = vm.Activo
            };
        }

        public static SaveUsuarioViewModel ToSaveViewModel(this UsuarioDto dto)
        {
            return new SaveUsuarioViewModel
            {
                Id = dto.Id,
                NombreCompleto = dto.NombreCompleto,
                NombreUsuario = dto.NombreUsuario,
                Cedula = dto.Cedula,
                Telefono = dto.Telefono,
                Rol = dto.Rol,
                PorcentajeComision = dto.PorcentajeComision,
                Activo = dto.Activo
            };
        }
        #endregion

        #region Categorías
        public static CategoriaViewModel ToViewModel(this CategoriaDto dto)
        {
            return new CategoriaViewModel
            {
                Id = dto.Id,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Activo = dto.Activo,
                FechaCreacion = dto.FechaCreacion,
                CantidadProductos = dto.CantidadProductos
            };
        }

        public static SaveCategoriaDto ToDto(this SaveCategoriaViewModel vm)
        {
            return new SaveCategoriaDto
            {
                Id = vm.Id,
                Nombre = vm.Nombre,
                Descripcion = vm.Descripcion
            };
        }

        public static SaveCategoriaViewModel ToSaveViewModel(this CategoriaDto dto)
        {
            return new SaveCategoriaViewModel
            {
                Id = dto.Id,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion
            };
        }
        #endregion

        #region Clientes
        public static ClienteViewModel ToViewModel(this ClienteDto dto)
        {
            return new ClienteViewModel
            {
                Id = dto.Id,
                Nombres = dto.Nombres,
                Apellidos = dto.Apellidos,
                CedulaORnc = dto.CedulaORnc,
                Telefono = dto.Telefono,
                Correo = dto.Correo,
                Direccion = dto.Direccion,
                Notas = dto.Notas,
                Activo = dto.Activo,
                FechaCreacion = dto.FechaCreacion
            };
        }

        public static SaveClienteDto ToDto(this SaveClienteViewModel vm)
        {
            return new SaveClienteDto
            {
                Id = vm.Id,
                Nombres = vm.Nombres,
                Apellidos = vm.Apellidos,
                CedulaORnc = vm.CedulaORnc,
                Telefono = vm.Telefono,
                Correo = vm.Correo,
                Direccion = vm.Direccion,
                Notas = vm.Notas
            };
        }

        public static SaveClienteViewModel ToSaveViewModel(this ClienteDto dto)
        {
            return new SaveClienteViewModel
            {
                Id = dto.Id,
                Nombres = dto.Nombres,
                Apellidos = dto.Apellidos,
                CedulaORnc = dto.CedulaORnc,
                Telefono = dto.Telefono,
                Correo = dto.Correo,
                Direccion = dto.Direccion,
                Notas = dto.Notas
            };
        }
        #endregion

        #region Productos & Variantes
        public static ProductoViewModel ToViewModel(this ProductoDto dto)
        {
            return new ProductoViewModel
            {
                Id = dto.Id,
                CodigoModelo = dto.CodigoModelo,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                CategoriaId = dto.CategoriaId,
                NombreCategoria = dto.NombreCategoria,
                PrecioVentaBase = dto.PrecioVentaBase,
                PrecioOferta = dto.PrecioOferta,
                CostoBaseReferencia = dto.CostoBaseReferencia,
                AplicaImpuesto = dto.AplicaImpuesto,
                PorcentajeImpuesto = dto.PorcentajeImpuesto,
                ImagenUrl = dto.ImagenUrl,
                Marca = dto.Marca,
                MaterialPrincipal = dto.MaterialPrincipal,
                Activo = dto.Activo,
                FechaCreacion = dto.FechaCreacion,
                Variantes = dto.Variantes.Select(v => new ProductoVarianteViewModel
                {
                    Id = v.Id,
                    ProductoId = v.ProductoId,
                    Sku = v.Sku,
                    CodigoBarras = v.CodigoBarras,
                    Talla = v.Talla,
                    Color = v.Color,
                    StockActual = v.StockActual,
                    StockMinimoAlerta = v.StockMinimoAlerta,
                    Activo = v.Activo,
                    NombreProducto = dto.Nombre,
                    CodigoModelo = dto.CodigoModelo,
                    PrecioVenta = dto.PrecioVentaBase,
                    PrecioOferta = dto.PrecioOferta
                }).ToList()
            };
        }

        public static SaveProductoDto ToDto(this SaveProductoViewModel vm)
        {
            return new SaveProductoDto
            {
                Id = vm.Id,
                CodigoModelo = vm.CodigoModelo,
                Nombre = vm.Nombre,
                Descripcion = vm.Descripcion,
                CategoriaId = vm.CategoriaId,
                PrecioVentaBase = vm.PrecioVentaBase,
                PrecioOferta = vm.PrecioOferta,
                CostoBaseReferencia = vm.CostoBaseReferencia,
                AplicaImpuesto = vm.AplicaImpuesto,
                PorcentajeImpuesto = vm.PorcentajeImpuesto,
                ImagenUrl = vm.ImagenUrl,
                Marca = vm.Marca,
                MaterialPrincipal = vm.MaterialPrincipal,
                Variantes = vm.Variantes.Select(v => new SaveProductoVarianteDto
                {
                    Id = v.Id,
                    ProductoId = v.ProductoId,
                    Sku = v.Sku,
                    CodigoBarras = v.CodigoBarras,
                    Talla = v.Talla,
                    Color = v.Color,
                    StockActual = v.StockActual,
                    StockMinimoAlerta = v.StockMinimoAlerta
                }).ToList()
            };
        }

        public static SaveProductoViewModel ToSaveViewModel(this ProductoDto dto)
        {
            return new SaveProductoViewModel
            {
                Id = dto.Id,
                CodigoModelo = dto.CodigoModelo,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                CategoriaId = dto.CategoriaId,
                PrecioVentaBase = dto.PrecioVentaBase,
                PrecioOferta = dto.PrecioOferta,
                CostoBaseReferencia = dto.CostoBaseReferencia,
                AplicaImpuesto = dto.AplicaImpuesto,
                PorcentajeImpuesto = dto.PorcentajeImpuesto,
                ImagenUrl = dto.ImagenUrl,
                Marca = dto.Marca,
                MaterialPrincipal = dto.MaterialPrincipal,
                Variantes = dto.Variantes.Select(v => new SaveProductoVarianteViewModel
                {
                    Id = v.Id,
                    ProductoId = v.ProductoId,
                    Sku = v.Sku,
                    CodigoBarras = v.CodigoBarras,
                    Talla = v.Talla,
                    Color = v.Color,
                    StockActual = v.StockActual,
                    StockMinimoAlerta = v.StockMinimoAlerta
                }).ToList()
            };
        }
        #endregion

        #region Caja & Turnos
        public static TurnoCajaViewModel ToViewModel(this TurnoCajaDto dto)
        {
            return new TurnoCajaViewModel
            {
                Id = dto.Id,
                CodigoTurno = dto.CodigoTurno,
                UsuarioId = dto.UsuarioId,
                NombreUsuario = dto.NombreUsuario,
                FechaApertura = dto.FechaApertura,
                FechaCierre = dto.FechaCierre,
                FondoInicialEfectivo = dto.FondoInicialEfectivo,
                TotalVentasEfectivo = dto.TotalVentasEfectivo,
                TotalVentasTarjeta = dto.TotalVentasTarjeta,
                TotalVentasTransferencia = dto.TotalVentasTransferencia,
                TotalGastosEfectivo = dto.TotalGastosEfectivo,
                EfectivoContadoAlCierre = dto.EfectivoContadoAlCierre,
                DiferenciaArqueo = dto.DiferenciaArqueo,
                Estado = dto.Estado,
                Observaciones = dto.Observaciones,
                Activo = dto.Activo,
                FechaCreacion = dto.FechaCreacion
            };
        }

        public static TurnoCajaDto ToDto(this TurnoCaja entity)
        {
            return new TurnoCajaDto
            {
                Id = entity.Id,
                CodigoTurno = entity.CodigoTurno,
                UsuarioId = entity.UsuarioId,
                NombreUsuario = entity.Usuario != null ? entity.Usuario.NombreCompleto : "Cajero",
                FechaApertura = entity.FechaApertura,
                FechaCierre = entity.FechaCierre,
                FondoInicialEfectivo = entity.FondoInicialEfectivo,
                TotalVentasEfectivo = entity.Ventas != null ? entity.Ventas.Where(v => v.Activo).SelectMany(v => v.Pagos ?? Enumerable.Empty<PagoVenta>()).Where(p => p != null && p.MetodoPago == MetodoPago.Efectivo).Sum(p => p.Monto) : 0m,
                TotalVentasTarjeta = entity.Ventas != null ? entity.Ventas.Where(v => v.Activo).SelectMany(v => v.Pagos ?? Enumerable.Empty<PagoVenta>()).Where(p => p != null && (p.MetodoPago == MetodoPago.TarjetaCredito || p.MetodoPago == MetodoPago.TarjetaDebito)).Sum(p => p.Monto) : 0m,
                TotalVentasTransferencia = entity.Ventas != null ? entity.Ventas.Where(v => v.Activo).SelectMany(v => v.Pagos ?? Enumerable.Empty<PagoVenta>()).Where(p => p != null && p.MetodoPago == MetodoPago.TransferenciaBancaria).Sum(p => p.Monto) : 0m,
                TotalGastosEfectivo = entity.Gastos != null ? entity.Gastos.Where(g => g.Activo).Sum(g => g.Monto) : 0m,
                EfectivoContadoAlCierre = entity.EfectivoContadoAlCierre,
                DiferenciaArqueo = entity.DiferenciaArqueo,
                Estado = entity.Estado,
                Observaciones = entity.Observaciones,
                Activo = entity.Activo,
                FechaCreacion = entity.FechaCreacion
            };
        }

        public static AbrirTurnoDto ToDto(this AbrirTurnoViewModel vm)
        {
            return new AbrirTurnoDto
            {
                FondoInicialEfectivo = vm.FondoInicialEfectivo,
                Observaciones = vm.Observaciones
            };
        }

        public static CerrarTurnoDto ToDto(this CerrarTurnoViewModel vm)
        {
            return new CerrarTurnoDto
            {
                TurnoId = vm.TurnoId,
                EfectivoContadoAlCierre = vm.EfectivoContadoAlCierre,
                Observaciones = vm.Observaciones
            };
        }

        public static GastoCajaViewModel ToViewModel(this GastoCajaDto dto)
        {
            return new GastoCajaViewModel
            {
                Id = dto.Id,
                TurnoCajaId = dto.TurnoCajaId,
                UsuarioId = dto.UsuarioId,
                NombreUsuario = dto.NombreUsuario,
                Concepto = dto.Concepto,
                Monto = dto.Monto,
                CategoriaGasto = dto.CategoriaGasto,
                MetodoPago = dto.MetodoPago,
                Observaciones = dto.Observaciones,
                Activo = dto.Activo,
                FechaCreacion = dto.FechaCreacion
            };
        }

        public static SaveGastoCajaDto ToDto(this SaveGastoCajaViewModel vm)
        {
            return new SaveGastoCajaDto
            {
                TurnoCajaId = vm.TurnoCajaId,
                Concepto = vm.Concepto,
                Monto = vm.Monto,
                CategoriaGasto = vm.CategoriaGasto,
                MetodoPago = vm.MetodoPago,
                Observaciones = vm.Observaciones
            };
        }
        #endregion

        #region Inventario
        public static EntradaMercanciaRapidaDto ToDto(this EntradaMercanciaRapidaViewModel vm)
        {
            return new EntradaMercanciaRapidaDto
            {
                ProductoVarianteId = vm.ProductoVarianteId,
                Cantidad = vm.Cantidad,
                Motivo = vm.Motivo,
                DocumentoReferencia = vm.DocumentoReferencia
            };
        }

        public static AjusteInventarioDto ToDto(this AjusteInventarioViewModel vm)
        {
            return new AjusteInventarioDto
            {
                ProductoVarianteId = vm.ProductoVarianteId,
                StockRealContado = vm.StockRealContado,
                Motivo = vm.Motivo
            };
        }

        public static MovimientoInventarioViewModel ToViewModel(this MovimientoInventarioDto dto)
        {
            return new MovimientoInventarioViewModel
            {
                Id = dto.Id,
                ProductoVarianteId = dto.ProductoVarianteId,
                Sku = dto.Sku,
                NombreProducto = dto.NombreProducto,
                Talla = dto.Talla,
                Color = dto.Color,
                UsuarioId = dto.UsuarioId,
                NombreUsuario = dto.NombreUsuario,
                TipoMovimiento = dto.TipoMovimiento,
                Cantidad = dto.Cantidad,
                StockAnterior = dto.StockAnterior,
                StockPosterior = dto.StockPosterior,
                CostoUnitarioReferencia = dto.CostoUnitarioReferencia,
                DocumentoReferencia = dto.DocumentoReferencia,
                Motivo = dto.Motivo,
                Activo = dto.Activo,
                FechaCreacion = dto.FechaCreacion
            };
        }
        #endregion

        #region Ventas
        public static VentaViewModel ToViewModel(this VentaDto dto)
        {
            return new VentaViewModel
            {
                Id = dto.Id,
                NumeroFactura = dto.NumeroFactura,
                Ncf = dto.Ncf,
                TipoComprobante = dto.TipoComprobante,
                ClienteId = dto.ClienteId,
                NombreCliente = dto.NombreCliente,
                UsuarioId = dto.UsuarioId,
                NombreUsuario = dto.NombreUsuario,
                TurnoCajaId = dto.TurnoCajaId,
                FechaVenta = dto.FechaVenta,
                Subtotal = dto.Subtotal,
                DescuentoTotal = dto.DescuentoTotal,
                ImpuestoTotal = dto.ImpuestoTotal,
                TotalNeto = dto.TotalNeto,
                Estado = dto.Estado,
                Observaciones = dto.Observaciones,
                Activo = dto.Activo,
                FechaCreacion = dto.FechaCreacion,
                Detalles = dto.Detalles.Select(d => new DetalleVentaViewModel
                {
                    Id = d.Id,
                    VentaId = d.VentaId,
                    ProductoVarianteId = d.ProductoVarianteId,
                    SkuSnapshot = d.SkuSnapshot,
                    NombreProductoSnapshot = d.NombreProductoSnapshot,
                    TallaSnapshot = d.TallaSnapshot,
                    ColorSnapshot = d.ColorSnapshot,
                    Cantidad = d.Cantidad,
                    CostoUnitario = d.CostoUnitario,
                    PrecioUnitario = d.PrecioUnitario,
                    PorcentajeDescuento = d.PorcentajeDescuento,
                    MontoDescuento = d.MontoDescuento,
                    MontoImpuesto = d.MontoImpuesto,
                    SubtotalLinea = d.SubtotalLinea,
                    Activo = d.Activo,
                    FechaCreacion = d.FechaCreacion
                }).ToList(),
                Pagos = dto.Pagos.Select(p => new PagoVentaViewModel
                {
                    Id = p.Id,
                    VentaId = p.VentaId,
                    MetodoPago = p.MetodoPago,
                    Monto = p.Monto,
                    ReferenciaTransaccion = p.ReferenciaTransaccion,
                    Activo = p.Activo,
                    FechaCreacion = p.FechaCreacion
                }).ToList()
            };
        }
        #endregion
    }
}
