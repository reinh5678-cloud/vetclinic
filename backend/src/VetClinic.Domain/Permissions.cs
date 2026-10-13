namespace VetClinic.Domain;

public sealed record PermissionInfo(string Code, string Group, string Label);

public static class Permissions
{
    public const string ClaimType = "permission";

    public static string PolicyName(string permission) => $"perm:{permission}";

    public const string PatientsView = "patients.view";
    public const string PatientsManage = "patients.manage";
    public const string AppointmentsView = "appointments.view";
    public const string AppointmentsManage = "appointments.manage";
    public const string MedicalView = "medical.view";
    public const string MedicalManage = "medical.manage";
    public const string HospitalizationView = "hospitalization.view";
    public const string HospitalizationManage = "hospitalization.manage";
    public const string InventoryView = "inventory.view";
    public const string InventoryManage = "inventory.manage";
    public const string InvoicesView = "invoices.view";
    public const string InvoicesManage = "invoices.manage";
    public const string ReportsView = "reports.view";

    public static readonly string[] All =
    {
        PatientsView, PatientsManage,
        AppointmentsView, AppointmentsManage,
        MedicalView, MedicalManage,
        HospitalizationView, HospitalizationManage,
        InventoryView, InventoryManage,
        InvoicesView, InvoicesManage,
        ReportsView
    };

    public static readonly IReadOnlyList<PermissionInfo> Catalog = new[]
    {
        new PermissionInfo(PatientsView, "Pacientes", "Ver pacientes"),
        new PermissionInfo(PatientsManage, "Pacientes", "Gestionar pacientes (crear, editar)"),
        new PermissionInfo(AppointmentsView, "Agenda", "Ver citas"),
        new PermissionInfo(AppointmentsManage, "Agenda", "Gestionar citas (crear, reprogramar)"),
        new PermissionInfo(MedicalView, "Historia clínica", "Ver historia clínica"),
        new PermissionInfo(MedicalManage, "Historia clínica", "Registrar y editar historia clínica"),
        new PermissionInfo(HospitalizationView, "Hospitalización", "Ver hospitalizaciones"),
        new PermissionInfo(HospitalizationManage, "Hospitalización", "Gestionar hospitalizaciones"),
        new PermissionInfo(InventoryView, "Inventario", "Ver inventario"),
        new PermissionInfo(InventoryManage, "Inventario", "Gestionar inventario (altas, ajustes)"),
        new PermissionInfo(InvoicesView, "Facturación", "Ver facturas"),
        new PermissionInfo(InvoicesManage, "Facturación", "Emitir y cobrar facturas"),
        new PermissionInfo(ReportsView, "Reportes", "Ver reportes y dashboard financiero")
    };
}
