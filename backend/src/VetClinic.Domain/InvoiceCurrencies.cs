namespace VetClinic.Domain;

/// <summary>
/// Moneda de la clínica. Toda la facturación se emite en soles (PEN); el campo
/// queda en la factura para dejar constancia explícita del importe y para
/// admitir más monedas en el futuro sin cambiar el modelo.
/// </summary>
public static class InvoiceCurrencies
{
    public const string Default = "PEN";

    public static readonly IReadOnlyList<string> All = new[] { "PEN" };

    public static bool IsValid(string? currency) =>
        !string.IsNullOrWhiteSpace(currency)
        && All.Contains(currency.Trim(), StringComparer.OrdinalIgnoreCase);
}