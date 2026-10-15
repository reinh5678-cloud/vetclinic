using VetClinic.Domain.Common;

namespace VetClinic.Domain.Entities;

/// <summary>
/// Configuración de la aplicación persistida en base de datos.
/// Una fila por sección; <see cref="Json"/> guarda el objeto completo serializado.
/// </summary>
public class AppSetting : BaseEntity
{
    public string Section { get; set; } = string.Empty;

    public string Json { get; set; } = "{}";

    public static class Sections
    {
        public const string Clinic = "clinic";
        public const string Billing = "billing";
        public const string Agenda = "agenda";
        public const string Inventory = "inventory";
        public const string Security = "security";
        public const string Notifications = "notifications";
    }
}
