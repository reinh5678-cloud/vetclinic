using Microsoft.Extensions.DependencyInjection;
using VetClinic.Application.Interfaces;
using VetClinic.Application.Services;

namespace VetClinic.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IRecoveryLockout, RecoveryLockout>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IMedicalRecordService, MedicalRecordService>();
        services.AddScoped<IHospitalizationService, HospitalizationService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IVaccineService, VaccineService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ISettingsService, SettingsService>();
        return services;
    }
}