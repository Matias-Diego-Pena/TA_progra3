using AdmitUWA.Data;
using AdmitUWA.Models;

namespace AdmitUWA.Services;

/// <summary>
/// Única fuente de datos del cascarón: una copia de los datos de ejemplo por sesión.
/// Concentra las reglas del proceso (historial y carné automáticos, decisiones del evaluador)
/// para que todas las pantallas vean el mismo estado. Se reemplazará por servicios que llamen a la API Java.
/// </summary>
public sealed class DatosSesion
{
    public const string Sistema = "Sistema";

    private readonly DatosDemo d;

    public DatosSesion()
    {
        Hoy = DateOnly.FromDateTime(DateTime.Today);
        d = new DatosDemo(Hoy);
    }

    /// <summary>Se dispara cuando cambia algo que afecta a contadores o menús.</summary>
    public event Action? Cambio;

    public DateOnly Hoy { get; }
    public int PostulanteActualId => DatosDemo.PostulanteActualId;
    public int EvaluadorActualId => DatosDemo.EvaluadorActualId;
    public string NombreAdministrador => "Admin Admisión";

    public List<Facultad> Facultades => d.Facultades;
    public List<Carrera> Carreras => d.Carreras;
    public List<Sede> Sedes => d.Sedes;
    public List<Modalidad> Modalidades => d.Modalidades;
    public List<Convocatoria> Convocatorias => d.Convocatorias;
    public List<OfertaCarrera> Ofertas => d.Ofertas;
    public List<RequisitoAplicable> Requisitos => d.Requisitos;
    public List<Postulante> Postulantes => d.Postulantes;
    public List<Evaluador> Evaluadores => d.Evaluadores;
    public List<Postulacion> Postulaciones => d.Postulaciones;
    public List<Pago> Pagos => d.Pagos;
    public List<Voucher> Vouchers => d.Vouchers;
    public List<Documento> Documentos => d.Documentos;
    public List<Observacion> Observaciones => d.Observaciones;
    public List<MovimientoEstado> Historial => d.Historial;
    public List<Carne> Carnes => d.Carnes;
    public List<Antecedente> Antecedentes => d.Antecedentes;
    public List<DocumentoPersonal> DocumentosPersonales => d.DocumentosPersonales;
    public List<Notificacion> Notificaciones => d.Notificaciones;

    // ---- Búsquedas -------------------------------------------------------
    public Facultad? Facultad(int id) => Facultades.FirstOrDefault(x => x.Id == id);
    public Carrera? Carrera(int id) => Carreras.FirstOrDefault(x => x.Id == id);
    public Sede? Sede(int id) => Sedes.FirstOrDefault(x => x.Id == id);
    public Modalidad? Modalidad(int id) => Modalidades.FirstOrDefault(x => x.Id == id);
    public Convocatoria? Convocatoria(int id) => Convocatorias.FirstOrDefault(x => x.Id == id);
    public Postulante? Postulante(int id) => Postulantes.FirstOrDefault(x => x.Id == id);
    public Postulacion? Postulacion(int id) => Postulaciones.FirstOrDefault(x => x.Id == id);
    public Evaluador? Evaluador(int? id) => id is null ? null : Evaluadores.FirstOrDefault(x => x.Id == id);
    public Documento? Documento(int id) => Documentos.FirstOrDefault(x => x.Id == id);

    public Postulante? PersonaDe(Postulacion? p) => p is null ? null : Postulante(p.PostulanteId);
    public Postulante? PersonaDePostulacion(int postulacionId) => PersonaDe(Postulacion(postulacionId));
    public Pago? PagoDe(int postulacionId) => Pagos.FirstOrDefault(x => x.PostulacionId == postulacionId);
    public List<Documento> DocumentosDe(int postulacionId) => Documentos.Where(x => x.PostulacionId == postulacionId).ToList();
    public List<Voucher> VouchersDe(int postulacionId) => Vouchers.Where(x => x.PostulacionId == postulacionId).OrderByDescending(x => x.Id).ToList();
    public List<MovimientoEstado> HistorialDe(int postulacionId) => Historial.Where(x => x.PostulacionId == postulacionId).OrderBy(x => x.Fecha).ThenBy(x => x.Id).ToList();
    public Carne? CarneVigenteDe(int postulacionId) => Carnes.FirstOrDefault(x => x.PostulacionId == postulacionId && x.Estado == EstadoCarne.Vigente);
    public List<Observacion> ObservacionesDe(int documentoId) => Observaciones.Where(o => o.DocumentoId == documentoId).OrderByDescending(o => o.Id).ToList();

    public Convocatoria? ConvocatoriaVigente => Convocatorias.FirstOrDefault(c => c.Estado == EstadoConvocatoria.Publicada);

    /// <summary>Convocatoria con la que trabajan por defecto listados y reportes: la vigente, o la más reciente.</summary>
    public int ConvocatoriaPorDefecto => (ConvocatoriaVigente ?? Convocatorias.OrderByDescending(c => c.Inicio).FirstOrDefault())?.Id ?? 0;

    public Postulante PostulanteActual => Postulante(PostulanteActualId)!;
    public Evaluador EvaluadorActual => Evaluador(EvaluadorActualId)!;

    public IEnumerable<Postulacion> PostulacionesDeConvocatoria(int convocatoriaId) =>
        Postulaciones.Where(p => convocatoriaId == 0 || p.ConvocatoriaId == convocatoriaId);

    public Postulacion? PostulacionActualDelPostulante() =>
        Postulaciones.Where(p => p.PostulanteId == PostulanteActualId)
            .OrderByDescending(p => p.ConvocatoriaId == ConvocatoriaVigente?.Id)
            .ThenByDescending(p => p.FechaRegistro)
            .FirstOrDefault();

    public int VacantesDe(int convocatoriaId, int carreraId) =>
        Ofertas.FirstOrDefault(o => o.ConvocatoriaId == convocatoriaId && o.CarreraId == carreraId)?.Vacantes ?? 0;

    public List<RequisitoAplicable> RequisitosPara(int convocatoriaId, int modalidadId) =>
        Requisitos.Where(r => r.ConvocatoriaId == convocatoriaId && r.ModalidadId == modalidadId && r.Activo).ToList();

    public List<Hito> HitosDe(Convocatoria c)
    {
        var hitos = new List<Hito> { new("Apertura de inscripciones", c.Inicio, "Inicio del registro de postulaciones.") };
        if (c.CierreInscripciones is { } cierre) hitos.Add(new("Cierre de inscripciones", cierre, "Último día para enviar la postulación y el voucher."));
        if (c.LimiteEvaluacion is { } limite) hitos.Add(new("Fin de evaluación de expedientes", limite, "Fecha límite para que los evaluadores resuelvan."));
        hitos.Add(new("Publicación de resultados", c.Fin, "Lista de admitidos por carrera y modalidad."));
        return hitos;
    }

    // ---- Reglas del expediente --------------------------------------------
    public bool DocumentosCompletos(int postulacionId)
    {
        var docs = DocumentosDe(postulacionId);
        return docs.Count > 0 && docs.All(x => x.Estado == EstadoDocumento.Aprobado);
    }

    public bool PagoAprobado(int postulacionId) => PagoDe(postulacionId)?.Estado == EstadoPago.Aprobado;

    /// <summary>Motivo por el que el expediente aún no puede declararse apto; null si puede.</summary>
    public string? MotivoNoApta(Postulacion p)
    {
        if (!DocumentosCompletos(p.Id)) return "Todos los documentos deben estar aprobados.";
        if (!PagoAprobado(p.Id)) return "El pago de inscripción debe estar validado.";
        return null;
    }

    public bool AptaSinCarne(Postulacion p) =>
        p.Estado == EstadoPostulacion.Apta && PagoAprobado(p.Id) && CarneVigenteDe(p.Id) is null;

    // ---- Operaciones ----------------------------------------------------------
    /// <summary>Cambia el estado y registra el movimiento en el historial (el historial no se edita a mano).</summary>
    public void CambiarEstado(Postulacion p, EstadoPostulacion nuevo, string motivo, string responsable)
    {
        if (p.Estado == nuevo) return;
        Historial.Add(new MovimientoEstado
        {
            Id = SiguienteId(Historial, x => x.Id), PostulacionId = p.Id, Anterior = p.Estado, Nuevo = nuevo,
            Motivo = motivo, Responsable = responsable, Fecha = DateTime.Now
        });
        p.Estado = nuevo;
        Notificar(RolUsuario.Postulante, $"Tu postulación ahora está: {nuevo.Etiqueta().ToLowerInvariant()}", motivo, "Postulación", p.PostulanteId);
        Avisar();
    }

    public void AprobarDocumento(Documento doc, string responsable)
    {
        doc.Estado = EstadoDocumento.Aprobado;
        doc.Comentario = null;
        foreach (var o in Observaciones.Where(o => o.DocumentoId == doc.Id && o.Estado == EstadoObservacion.Abierta))
            o.Estado = EstadoObservacion.Subsanada;
        IniciarRevision(doc.PostulacionId, responsable);
        Avisar();
    }

    public void ObservarDocumento(Documento doc, TipoObservacion tipo, string descripcion, int? evaluadorId, string responsable)
    {
        doc.Estado = EstadoDocumento.Observado;
        doc.Comentario = descripcion;
        Observaciones.Add(new Observacion
        {
            Id = SiguienteId(Observaciones, x => x.Id), DocumentoId = doc.Id, EvaluadorId = evaluadorId,
            Tipo = tipo, Descripcion = descripcion, Fecha = Hoy
        });
        IniciarRevision(doc.PostulacionId, responsable);
        Avisar();
    }

    private void IniciarRevision(int postulacionId, string responsable)
    {
        if (Postulacion(postulacionId) is { Estado: EstadoPostulacion.Enviada } p)
            CambiarEstado(p, EstadoPostulacion.EnRevision, "Inicio de la revisión documental", responsable);
    }

    /// <summary>El postulante sube una nueva versión de un documento observado.</summary>
    public void SubirNuevaVersion(Documento doc, string archivo)
    {
        doc.Archivo = archivo;
        doc.Version++;
        doc.FechaCarga = Hoy;
        doc.Estado = EstadoDocumento.Pendiente;
        doc.Comentario = null;
        foreach (var o in Observaciones.Where(o => o.DocumentoId == doc.Id && o.Estado == EstadoObservacion.Abierta))
            o.Estado = EstadoObservacion.Subsanada;
        var p = Postulacion(doc.PostulacionId);
        if (p is { Estado: EstadoPostulacion.Observada } && DocumentosDe(p.Id).All(x => x.Estado != EstadoDocumento.Observado))
            CambiarEstado(p, EstadoPostulacion.EnRevision, "Documentos subsanados por el postulante", PersonaDe(p)?.NombreCompleto ?? Sistema);
        Notificar(RolUsuario.Evaluador, "Documento reemplazado", $"{PersonaDe(p)?.NombreCompleto} cargó una nueva versión de: {doc.Requisito}.", "Documento");
        Avisar();
    }

    /// <summary>Decisión del evaluador sobre el expediente. Devuelve un mensaje de error si no procede.</summary>
    public string? Decidir(Postulacion p, EstadoPostulacion decision, string comentario, string responsable)
    {
        if (!p.Estado.EsPendiente()) return "La postulación ya fue resuelta.";
        if (decision == EstadoPostulacion.Apta && MotivoNoApta(p) is { } motivo) return motivo;
        if (decision != EstadoPostulacion.Apta && string.IsNullOrWhiteSpace(comentario))
            return "Escribe un comentario que explique la decisión al postulante.";
        p.Observacion = decision == EstadoPostulacion.Apta ? null : comentario.Trim();
        CambiarEstado(p, decision, decision == EstadoPostulacion.Apta ? "Expediente completo y conforme" : comentario.Trim(), responsable);
        EvaluarEmisionCarne(p);
        return null;
    }

    public void AsignarEvaluador(Postulacion p, int evaluadorId)
    {
        p.EvaluadorId = evaluadorId;
        Notificar(RolUsuario.Evaluador, "Nueva postulación asignada", $"Se te asignó la postulación {p.Codigo}.", "Postulación");
        Avisar();
    }

    // ---- Vouchers y pagos -------------------------------------------------------
    public void RegistrarVoucher(Postulacion p, string codigoOperacion, decimal monto, string archivo)
    {
        Vouchers.Add(new Voucher
        {
            Id = SiguienteId(Vouchers, x => x.Id), PostulacionId = p.Id, CodigoOperacion = codigoOperacion.Trim(),
            Monto = monto, Archivo = archivo, FechaRegistro = Hoy
        });
        if (PagoDe(p.Id) is { } pago && pago.Estado != EstadoPago.Aprobado) pago.Estado = EstadoPago.Pendiente;
        Notificar(RolUsuario.Administrador, "Voucher por validar", $"Se registró un voucher de {Formato.Soles(monto)} para {p.Codigo}.", "Pago");
        Avisar();
    }

    public void RetirarVoucher(Voucher v)
    {
        Vouchers.Remove(v);
        if (PagoDe(v.PostulacionId) is { Estado: EstadoPago.Pendiente } pago && !VouchersDe(v.PostulacionId).Any(x => x.Estado == EstadoVoucher.Pendiente))
            pago.Estado = EstadoPago.Generado;
        Avisar();
    }

    public void AprobarVoucher(Voucher v, string responsable)
    {
        v.Estado = EstadoVoucher.Aprobado;
        v.FechaRevision = Hoy;
        v.MotivoRechazo = null;
        if (PagoDe(v.PostulacionId) is { } pago)
        {
            pago.Estado = EstadoPago.Aprobado;
            pago.FechaAprobacion = Hoy;
        }
        var p = Postulacion(v.PostulacionId);
        if (p is not null)
        {
            Notificar(RolUsuario.Postulante, "Pago validado", $"Tu pago de {Formato.Soles(v.Monto)} fue validado.", "Pago", p.PostulanteId);
            EvaluarEmisionCarne(p);
        }
        Avisar();
    }

    public void RechazarVoucher(Voucher v, string motivo)
    {
        v.Estado = EstadoVoucher.Rechazado;
        v.FechaRevision = Hoy;
        v.MotivoRechazo = motivo.Trim();
        if (PagoDe(v.PostulacionId) is { } pago && pago.Estado != EstadoPago.Aprobado) pago.Estado = EstadoPago.Rechazado;
        if (Postulacion(v.PostulacionId) is { } p)
            Notificar(RolUsuario.Postulante, "Voucher rechazado", $"Motivo: {motivo.Trim()} Adjunta un nuevo voucher.", "Pago", p.PostulanteId);
        Avisar();
    }

    // ---- Carnés --------------------------------------------------------------
    /// <summary>Emite el carné automáticamente cuando el expediente es apto y el pago está validado.</summary>
    public void EvaluarEmisionCarne(Postulacion p)
    {
        if (AptaSinCarne(p)) EmitirCarne(p, automatico: true, motivo: null);
    }

    public Carne EmitirCarne(Postulacion p, bool automatico, string? motivo, int? sedeId = null, string? aula = null)
    {
        var id = SiguienteId(Carnes, x => x.Id);
        var periodo = Convocatoria(p.ConvocatoriaId)?.Periodo ?? Hoy.Year.ToString();
        var carne = new Carne
        {
            Id = id, PostulacionId = p.Id, Codigo = $"CAR-{periodo}-{id:0000}",
            SedeId = sedeId ?? p.SedeId, Aula = aula ?? $"A-{101 + p.Id % 12}",
            FechaEmision = Hoy, Automatico = automatico, Motivo = motivo
        };
        Carnes.Add(carne);
        Notificar(RolUsuario.Postulante, "Tu carné de postulante está disponible", $"Carné {carne.Codigo}: preséntalo con tu documento el día de la evaluación.", "Postulación", p.PostulanteId);
        Avisar();
        return carne;
    }

    public void AnularCarne(Carne c, string motivo)
    {
        c.Estado = EstadoCarne.Anulado;
        c.Motivo = motivo.Trim();
        Avisar();
    }

    /// <summary>Anula el carné y emite uno nuevo con la sede o aula corregida.</summary>
    public Carne? ReemitirCarne(Carne c, int sedeId, string aula, string motivo)
    {
        var p = Postulacion(c.PostulacionId);
        if (p is null) return null;
        AnularCarne(c, $"Reemitido: {motivo.Trim()}");
        return EmitirCarne(p, automatico: false, motivo: motivo.Trim(), sedeId: sedeId, aula: aula.Trim());
    }

    // ---- Notificaciones ------------------------------------------------------
    public IEnumerable<Notificacion> NotificacionesDe(RolUsuario rol) => Notificaciones.Where(n => n.Rol == rol).OrderByDescending(n => n.Fecha);
    public int SinLeer(RolUsuario rol) => Notificaciones.Count(n => n.Rol == rol && !n.Leida);

    public void MarcarLeidas(RolUsuario rol)
    {
        foreach (var n in Notificaciones.Where(n => n.Rol == rol)) n.Leida = true;
        Avisar();
    }

    /// <summary>Crea una notificación. En el cascarón, las del postulante solo se muestran al postulante de la sesión.</summary>
    public void Notificar(RolUsuario rol, string titulo, string detalle, string tipo, int? postulanteId = null)
    {
        if (rol == RolUsuario.Postulante && postulanteId is not null && postulanteId != PostulanteActualId) return;
        Notificaciones.Add(new Notificacion
        {
            Id = SiguienteId(Notificaciones, x => x.Id), Rol = rol, Titulo = titulo, Detalle = detalle,
            Tipo = tipo, Medio = "Sistema", Fecha = DateTime.Now
        });
    }

    // ---- Utilidades -------------------------------------------------------
    public static int SiguienteId<T>(IEnumerable<T> elementos, Func<T, int> id) => elementos.Select(id).DefaultIfEmpty().Max() + 1;

    public void Avisar() => Cambio?.Invoke();
}
