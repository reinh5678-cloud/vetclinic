using System.Text.Json;
using System.Text.Json.Nodes;

namespace VetClinic.Api.Services;

public class LocalSettingsWriter
{
    private readonly string _path;

    public LocalSettingsWriter(IWebHostEnvironment environment)
        => _path = Path.Combine(environment.ContentRootPath, "appsettings.local.json");

    public void SetBillingTaxRate(decimal taxRate)
    {
        JsonObject root;
        if (File.Exists(_path))
        {
            var existing = File.ReadAllText(_path);
            root = string.IsNullOrWhiteSpace(existing)
                ? new JsonObject()
                : JsonNode.Parse(existing)?.AsObject() ?? new JsonObject();
        }
        else
        {
            root = new JsonObject();
        }

        if (root["Billing"] is not JsonObject billing)
        {
            billing = new JsonObject();
            root["Billing"] = billing;
        }

        billing["TaxRate"] = taxRate;

        File.WriteAllText(_path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
