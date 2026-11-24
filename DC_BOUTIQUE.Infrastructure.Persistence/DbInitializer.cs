using System.Security.Cryptography;
using System.Text;
using DC_BOUTIQUE.Core.Domain.Common.Enums;
using DC_BOUTIQUE.Core.Domain.Entities;
using DC_BOUTIQUE.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DC_BOUTIQUE.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(DCBoutiqueContext context)
        {
            if (context.Database.IsSqlite())
            {
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                await context.Database.MigrateAsync();
            }

            // 1. Usuarios base del sistema (Admin y Cajera)
            if (!await context.Usuarios.AnyAsync())
            {
                var admin = new Usuario
                {
                    Id = Guid.NewGuid(),
                    NombreCompleto = "Dahiana Corporan",
                    NombreUsuario = "admin",
                    PasswordHash = HashPassword("Admin123$"),
                    Cedula = "402-0000000-0",
                    Telefono = "809-555-0101",
                    Rol = RolUsuario.Administrador,
                    PorcentajeComision = 0m,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                var cajera = new Usuario
                {
                    Id = Guid.NewGuid(),
                    NombreCompleto = "Cajera Principal",
                    NombreUsuario = "caja01",
                    PasswordHash = HashPassword("Caja1234$"),
                    Cedula = "402-0000000-1",
                    Telefono = "809-555-0102",
                    Rol = RolUsuario.Vendedor,
                    PorcentajeComision = 2.0m,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                await context.Usuarios.AddRangeAsync(admin, cajera);
            }

            // 2. Cliente de Mostrador (Consumidor Final para ventas sin RNC)
            if (!await context.Clientes.AnyAsync())
            {
                var clienteFinal = new Cliente
                {
                    Id = Guid.NewGuid(),
                    Nombres = "Consumidor",
                    Apellidos = "Final",
                    CedulaORnc = null,
                    Telefono = "809-000-0000",
                    Correo = null,
                    Direccion = "Mostrador Boutique",
                    Notas = "Cliente general para ventas de mostrador al contado",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                await context.Clientes.AddAsync(clienteFinal);
            }

            // 3. Categorías estándar de la Boutique
            if (!await context.Categorias.AnyAsync())
            {
                var categorias = new List<Categoria>
                {
                    new() { Id = Guid.NewGuid(), Nombre = "Vestidos & Enterizos", Descripcion = "Vestidos casuales, de noche y cóctel", Activo = true, FechaCreacion = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), Nombre = "Blusas & Tops", Descripcion = "Camisas, blusas y tops en tendencia", Activo = true, FechaCreacion = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), Nombre = "Pantalones & Jeans", Descripcion = "Pantalones de vestir, jeans y palazzos", Activo = true, FechaCreacion = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), Nombre = "Conjuntos & Sets", Descripcion = "Sets de 2 y 3 piezas", Activo = true, FechaCreacion = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), Nombre = "Faldas & Shorts", Descripcion = "Faldas y shorts", Activo = true, FechaCreacion = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), Nombre = "Calzado", Descripcion = "Zapatos, sandalias y tacones", Activo = true, FechaCreacion = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), Nombre = "Carteras & Bolsos", Descripcion = "Bolsos de mano y carteras", Activo = true, FechaCreacion = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), Nombre = "Accesorios", Descripcion = "Cinturones y complementos", Activo = true, FechaCreacion = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), Nombre = "Otros", Descripcion = "Otras prendas, artículos varios y promociones", Activo = true, FechaCreacion = DateTime.UtcNow }
                };

                await context.Categorias.AddRangeAsync(categorias);
            }
            else if (!await context.Categorias.AnyAsync(c => c.Nombre == "Otros"))
            {
                await context.Categorias.AddAsync(new Categoria
                {
                    Id = Guid.NewGuid(),
                    Nombre = "Otros",
                    Descripcion = "Otras prendas, artículos varios y promociones",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();

            // 4. Limpieza del Catálogo: Retirar productos de la base de datos
            var detalles = await context.DetallesVenta.ToListAsync();
            if (detalles.Any()) context.DetallesVenta.RemoveRange(detalles);

            var movimientos = await context.MovimientosInventario.ToListAsync();
            if (movimientos.Any()) context.MovimientosInventario.RemoveRange(movimientos);

            var variantes = await context.ProductoVariantes.ToListAsync();
            if (variantes.Any()) context.ProductoVariantes.RemoveRange(variantes);

            var productos = await context.Productos.ToListAsync();
            if (productos.Any()) context.Productos.RemoveRange(productos);

            await context.SaveChangesAsync();
        }

        public static async Task SeedProductosDemoAsync(DCBoutiqueContext context)
        {
            var cats = await context.Categorias.ToListAsync();
            var catVestidos = cats.FirstOrDefault(c => c.Nombre.Contains("Vestido")) ?? cats.First();
            var catBlusas = cats.FirstOrDefault(c => c.Nombre.Contains("Blusa")) ?? cats.First();
            var catFaldas = cats.FirstOrDefault(c => c.Nombre.Contains("Falda")) ?? cats.First();
            var catPantalones = cats.FirstOrDefault(c => c.Nombre.Contains("Pantalon")) ?? cats.First();
            var catConjuntos = cats.FirstOrDefault(c => c.Nombre.Contains("Conjunto")) ?? cats.First();
            var catCalzado = cats.FirstOrDefault(c => c.Nombre.Contains("Calzado")) ?? cats.First();
            var catCarteras = cats.FirstOrDefault(c => c.Nombre.Contains("Cartera")) ?? cats.First();
            var catAccesorios = cats.FirstOrDefault(c => c.Nombre.Contains("Accesorio")) ?? cats.First();
            var catOtros = cats.FirstOrDefault(c => c.Nombre.Contains("Otro")) ?? cats.First();

            var productosDemo = new List<(Guid Id, string Codigo, string Nombre, Guid CategoriaId, string Material, string Descripcion, decimal Costo, decimal Precio, string Foto, (string Talla, string Color, string Sku, int Stock)[] Variantes)>
            {
                (
                    Guid.Parse("a1111111-1111-1111-1111-111111111111"),
                    "VES-001",
                    "Vestido Floral Midi",
                    catVestidos.Id,
                    "Chiffon de Seda",
                    "Vestido midi estampado floral con tirantes finos y caída elegante para cóctel o tarde casual.",
                    1200m,
                    2800m,
                    "vestido_floral_midi.jpg",
                    new[] { ("M", "Rosa", "VES-001-M-ROS", 8), ("S", "Rosa", "VES-001-S-ROS", 5) }
                ),
                (
                    Guid.Parse("a2222222-2222-2222-2222-222222222222"),
                    "VES-002",
                    "Vestido Satín Champagne",
                    catVestidos.Id,
                    "Satín Premium",
                    "Vestido slip dress en satín color champagne, escote fluido ideal para eventos y cenas.",
                    1800m,
                    3900m,
                    "vestido_satin_champagne.jpg",
                    new[] { ("S", "Champagne", "VES-002-S-CHA", 6), ("M", "Champagne", "VES-002-M-CHA", 4) }
                ),
                (
                    Guid.Parse("a3333333-3333-3333-3333-333333333333"),
                    "BLU-001",
                    "Top Básico Blanco",
                    catBlusas.Id,
                    "Algodón Pima",
                    "Top crop básico en suave algodón blanco de alta densidad, infaltable para combinar.",
                    300m,
                    750m,
                    "top_basico_blanco.jpg",
                    new[] { ("M", "Blanco", "BLU-001-M-BLA", 12), ("S", "Blanco", "BLU-001-S-BLA", 8) }
                ),
                (
                    Guid.Parse("a4444444-4444-4444-4444-444444444444"),
                    "BLU-002",
                    "Blusa de Lino Beige",
                    catBlusas.Id,
                    "Lino 100%",
                    "Blusa abotonada manga larga con cuello clásico en lino fresco tono arena natural.",
                    650m,
                    1500m,
                    "blusa_lino_beige.jpg",
                    new[] { ("M", "Beige", "BLU-002-M-BEI", 9), ("L", "Beige", "BLU-002-L-BEI", 7) }
                ),
                (
                    Guid.Parse("a5555555-5555-5555-5555-555555555555"),
                    "FAL-001",
                    "Falda Plisada Midi Negra",
                    catFaldas.Id,
                    "Poliéster Satinado",
                    "Falda midi plisada negra con pretina elástica de calce perfecto y movimiento fluido.",
                    700m,
                    1650m,
                    "falda_plisada_negra.jpg",
                    new[] { ("M", "Negro", "FAL-001-M-NEG", 10), ("S", "Negro", "FAL-001-S-NEG", 6) }
                ),
                (
                    Guid.Parse("a6666666-6666-6666-6666-666666666666"),
                    "FAL-002",
                    "Short Denim Claro",
                    catFaldas.Id,
                    "Denim 100% Algodón",
                    "Short vaquero tiro alto en lavado celeste claro con ruedo deshilachado.",
                    550m,
                    1250m,
                    "short_denim.jpg",
                    new[] { ("28", "Azul Claro", "FAL-002-28-AZU", 8), ("30", "Azul Claro", "FAL-002-30-AZU", 7) }
                ),
                (
                    Guid.Parse("a7777777-7777-7777-7777-777777777777"),
                    "PAN-001",
                    "Jeans Wide Leg",
                    catPantalones.Id,
                    "Denim Clásico",
                    "Jeans de pierna ancha tiro alto en azul tradicional, el corte favorito de la temporada.",
                    900m,
                    2200m,
                    "jeans_wide_leg.jpg",
                    new[] { ("30", "Azul", "PAN-001-30-AZU", 9), ("28", "Azul", "PAN-001-28-AZU", 5) }
                ),
                (
                    Guid.Parse("a8888888-8888-8888-8888-888888888888"),
                    "PAN-002",
                    "Palazzo de Lino Crema",
                    catPantalones.Id,
                    "Lino & Viscosa",
                    "Pantalón palazzo fluido en tono crema con elástico posterior y bolsillos laterales.",
                    800m,
                    1900m,
                    "palazzo_lino_crema.jpg",
                    new[] { ("M", "Crema", "PAN-002-M-CRE", 8), ("L", "Crema", "PAN-002-L-CRE", 4) }
                ),
                (
                    Guid.Parse("a9999999-9999-9999-9999-999999999999"),
                    "SET-001",
                    "Set Blazer y Pantalón Beige",
                    catConjuntos.Id,
                    "Crepe Sastre",
                    "Coordinado sastre de 2 piezas (blazer con botones carey y pantalón recto) para oficina o evento.",
                    2200m,
                    4800m,
                    "set_blazer_beige.jpg",
                    new[] { ("M", "Beige", "SET-001-M-BEI", 6), ("S", "Beige", "SET-001-S-BEI", 4) }
                ),
                (
                    Guid.Parse("baaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "SET-002",
                    "Conjunto Deportivo Lila",
                    catConjuntos.Id,
                    "Microfibra & Spandex",
                    "Set deportivo de 2 piezas (top cruzado y leggings tiro alto de compresión) en tono lila pastel.",
                    1100m,
                    2500m,
                    "conjunto_deportivo_lila.jpg",
                    new[] { ("S", "Lila", "SET-002-S-LIL", 8), ("M", "Lila", "SET-002-M-LIL", 8) }
                ),
                (
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    "CAL-001",
                    "Tenis Blancos Minimal",
                    catCalzado.Id,
                    "Cuero Sintético",
                    "Tenis urbanos blancos de silueta minimalista con plantilla acolchada para uso diario.",
                    1300m,
                    2900m,
                    "tenis_blancos.jpg",
                    new[] { ("38", "Blanco", "CAL-001-38-BLA", 6), ("37", "Blanco", "CAL-001-37-BLA", 5) }
                ),
                (
                    Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    "CAL-002",
                    "Tacones Stiletto Nude",
                    catCalzado.Id,
                    "Charol Sintético",
                    "Zapatos de tacón stiletto en charol nude clásico, tacón fino 9cm con punta estilizada.",
                    1500m,
                    3400m,
                    "tacones_nude.jpg",
                    new[] { ("37", "Nude", "CAL-002-37-NUD", 5), ("38", "Nude", "CAL-002-38-NUD", 4) }
                ),
                (
                    Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    "CAR-001",
                    "Bolso Trendy Marrón",
                    catCarteras.Id,
                    "Cuero Sintético",
                    "Bolso estructurado de mano con asas redondas y correa cruzada desmontable en color marrón coñac.",
                    800m,
                    1800m,
                    "",
                    new[] { ("ÚNICA", "Marrón", "CAR-001-U-MAR", 6) }
                ),
                (
                    Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    "CAR-002",
                    "Clutch de Fiesta Dorado",
                    catCarteras.Id,
                    "Textil Glitter",
                    "Cartera sobre rígida en tono dorado metalizado con cadena desmontable para cócteles y fiestas.",
                    650m,
                    1450m,
                    "",
                    new[] { ("ÚNICA", "Dorado", "CAR-002-U-DOR", 5) }
                ),
                (
                    Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                    "ACC-001",
                    "Aretes Argolla Dorados",
                    catAccesorios.Id,
                    "Acero Inoxidable",
                    "Argollas medianas tubulares con baño dorado hipoalergénico, acabado de alto brillo.",
                    200m,
                    550m,
                    "",
                    new[] { ("ÚNICA", "Dorado", "ACC-001-U-DOR", 15) }
                ),
                (
                    Guid.Parse("10101010-1010-1010-1010-101010101010"),
                    "ACC-002",
                    "Collar de Perlas Finas",
                    catAccesorios.Id,
                    "Perlas Cultivadas",
                    "Collar estilo gargantilla con perlas redondas finas y cierre marinero en chapa de oro.",
                    350m,
                    850m,
                    "",
                    new[] { ("ÚNICA", "Blanco Perla", "ACC-002-U-BLA", 10) }
                ),
                (
                    Guid.Parse("20202020-2020-2020-2020-202020202020"),
                    "OTR-001",
                    "Gafas de Sol Cat-Eye",
                    catOtros.Id,
                    "Acetato Negro",
                    "Gafas de sol de silueta felina con cristales oscuros filtro UV400 y marco negro brillante.",
                    300m,
                    750m,
                    "",
                    new[] { ("ÚNICA", "Negro", "OTR-001-U-NEG", 12) }
                ),
                (
                    Guid.Parse("30303030-3030-3030-3030-303030303030"),
                    "OTR-002",
                    "Pañuelo de Seda Estampado",
                    catOtros.Id,
                    "Seda Satín",
                    "Pañuelo cuadrado multifuncional con estampado gráfico elegante para cuello, cabello o bolso.",
                    250m,
                    600m,
                    "",
                    new[] { ("ÚNICA", "Multicolor", "OTR-002-U-MUL", 10) }
                )
            };

            foreach (var item in productosDemo)
            {
                var prodExistente = await context.Productos
                    .Include(p => p.Variantes)
                    .FirstOrDefaultAsync(p => p.CodigoModelo == item.Codigo || p.Id == item.Id);

                string imagenUrl = !string.IsNullOrEmpty(item.Foto)
                    ? $"/images/productos/{item.Id}/{item.Foto}"
                    : "/images/defaults/product-placeholder.png";

                if (prodExistente == null)
                {
                    var nuevoProd = new Producto
                    {
                        Id = item.Id,
                        CodigoModelo = item.Codigo,
                        Nombre = item.Nombre,
                        Descripcion = item.Descripcion,
                        Marca = "DC Boutique",
                        CategoriaId = item.CategoriaId,
                        MaterialPrincipal = item.Material,
                        ImagenUrl = imagenUrl,
                        CostoBaseReferencia = item.Costo,
                        PrecioVentaBase = item.Precio,
                        PorcentajeImpuesto = 18m,
                        AplicaImpuesto = true,
                        Activo = true,
                        FechaCreacion = DateTime.UtcNow
                    };

                    foreach (var v in item.Variantes)
                    {
                        nuevoProd.Variantes.Add(new ProductoVariante
                        {
                            Id = Guid.NewGuid(),
                            ProductoId = item.Id,
                            Sku = v.Sku,
                            Talla = v.Talla,
                            Color = v.Color,
                            StockActual = v.Stock,
                            StockMinimoAlerta = 2,
                            ConcurrencyToken = Guid.NewGuid(),
                            Activo = true,
                            FechaCreacion = DateTime.UtcNow
                        });
                    }

                    await context.Productos.AddAsync(nuevoProd);
                }
                else
                {
                    prodExistente.Activo = true;
                    if (string.IsNullOrWhiteSpace(prodExistente.ImagenUrl) && !string.IsNullOrEmpty(item.Foto))
                    {
                        prodExistente.ImagenUrl = imagenUrl;
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        private static string HashPassword(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
