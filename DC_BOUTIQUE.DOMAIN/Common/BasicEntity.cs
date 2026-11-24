using System;

namespace DC_BOUTIQUE.Core.Domain.Common
{
    public abstract class BasicEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public bool Activo { get; set; } = true;
    }
}
