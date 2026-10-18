using System.Text.Json;
using VetClinic.Api.Serialization;
using VetClinic.Application.DTOs;

namespace VetClinic.UnitTests;

/// <summary>
/// Npgsql rechaza escribir DateTime con Kind=Unspecified en columnas timestamptz.
/// Las fechas que llegan sin zona horaria ("2027-10-18") deben normalizarse a UTC
/// al deserializar, para no terminar en un 409 de conflicto al guardar.
/// </summary>
public class UtcDateTimeConverterTests
{
    private static readonly JsonSerializerOptions Options = BuildOptions();

    private static JsonSerializerOptions BuildOptions()
    {
        // Mismos defaults que usa ASP.NET Core (Web) + los converters de la API.
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        UtcDatesJson.AddTo(options);
        return options;
    }

    [Theory]
    [InlineData("\"2027-10-18\"", 2027, 10, 18, 0, 0, 0)]
    [InlineData("\"2027-10-18T00:00:00Z\"", 2027, 10, 18, 0, 0, 0)]
    [InlineData("\"2027-10-18T23:30:00\"", 2027, 10, 18, 23, 30, 0)]
    public void Deserializa_a_utc(string json, int y, int m, int d, int hh, int mm, int ss)
    {
        var value = JsonSerializer.Deserialize<DateTime>(json, Options);

        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.Equal(new DateTime(y, m, d, hh, mm, ss, DateTimeKind.Utc), value);
    }

    [Fact]
    public void Deserializa_desde_json_con_desplazamiento_a_utc()
    {
        var value = JsonSerializer.Deserialize<DateTime>("\"2027-10-18T00:00:00-05:00\"", Options);

        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.Equal(new DateTime(2027, 10, 18, 5, 0, 0, DateTimeKind.Utc), value);
    }

    [Fact]
    public void Deserializa_fecha_hora_sin_milisegundos_como_utc()
    {
        var value = JsonSerializer.Deserialize<DateTime>("\"2027-10-18T00:00:00\"", Options);

        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.Equal(0, value.Millisecond);
    }

    [Fact]
    public void Nullable_null_se_conserva_null()
    {
        var value = JsonSerializer.Deserialize<DateTime?>("null", Options);

        Assert.Null(value);
    }

    [Fact]
    public void Anotacion_de_salida_conserva_utc()
    {
        var json = JsonSerializer.Serialize(new DateTime(2027, 10, 18, 0, 0, 0, DateTimeKind.Utc), Options);

        Assert.Equal("\"2027-10-18T00:00:00Z\"", json);
    }

    [Fact]
    public void Lote_con_fecha_sin_hora_llega_utc()
    {
        var request = JsonSerializer.Deserialize<AddLotRequest>(
            "{\"lotNumber\":\"L-1\",\"expirationDate\":\"2027-06-30\",\"quantity\":10,\"description\":\"\"}", Options);

        Assert.NotNull(request);
        Assert.Equal(DateTimeKind.Utc, request!.ExpirationDate!.Value.Kind);
        Assert.Equal(new DateTime(2027, 6, 30, 0, 0, 0, DateTimeKind.Utc), request.ExpirationDate);
    }

    [Fact]
    public void Lote_sin_fecha_sigue_siendo_null()
    {
        var request = JsonSerializer.Deserialize<AddLotRequest>(
            "{\"lotNumber\":\"L-1\",\"expirationDate\":null,\"quantity\":10,\"description\":\"\"}", Options);

        Assert.NotNull(request);
        Assert.Null(request!.ExpirationDate);
    }

    [Fact]
    public void Vacuna_con_fechas_sin_hora_llega_utc()
    {
        var request = JsonSerializer.Deserialize<AddVaccineRequest>(
            "{\"inventoryItemId\":1,\"applicationDate\":\"2026-10-18\",\"nextDueDate\":\"2027-10-18\"}", Options);

        Assert.NotNull(request);
        Assert.Equal(DateTimeKind.Utc, request!.ApplicationDate.Kind);
        Assert.Equal(DateTimeKind.Utc, request.NextDueDate!.Value.Kind);
        Assert.Equal(new DateTime(2027, 10, 18, 0, 0, 0, DateTimeKind.Utc), request.NextDueDate);
    }

    [Fact]
    public void Medicacion_hospitalaria_con_fecha_sin_hora_llega_utc()
    {
        var request = JsonSerializer.Deserialize<AddMedicationAdministrationRequest>(
            "{\"medicationName\":\"Gasas\",\"dose\":\"1\",\"route\":\"Oral\",\"administeredAt\":\"2026-10-18T08:30\"}", Options);

        Assert.NotNull(request);
        Assert.Equal(DateTimeKind.Utc, request!.AdministeredAt.Kind);
        Assert.Equal(new DateTime(2026, 10, 18, 8, 30, 0, DateTimeKind.Utc), request.AdministeredAt);
    }
}