namespace DC_BOUTIQUE.Core.Domain.Common.Enums
{
    public enum TipoLogTecnico
    {
        Error = 1,
        Excepcion = 2,
        Persistencia = 3,
        Impresion = 4,
        Autenticacion = 5,
        Auditoria = 6,
        Sistema = 7,
        Ventas = 8,
        Respaldos = 9,
        Restauracion = 10
    }

    public enum FrecuenciaRespaldo
    {
        Desactivado = 0,
        Cada30Minutos = 30,
        Cada1Hora = 60,
        Cada2Horas = 120,
        Cada4Horas = 240,
        Cada6Horas = 360,
        Cada12Horas = 720,
        Diario = 1440
    }

    public enum TipoMantenimiento
    {
        CorreccionErrores = 1,
        ActualizacionSistema = 2,
        RestauracionRespaldo = 3,
        MantenimientoPreventivo = 4,
        ConfiguracionImpresora = 5,
        Diagnostico = 6,
        ModificacionTecnica = 7,
        Otro = 8
    }
}
