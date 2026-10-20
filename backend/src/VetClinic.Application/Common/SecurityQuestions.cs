using VetClinic.Application.DTOs;

namespace VetClinic.Application.Common;

public static class SecurityQuestions
{
    public static readonly IReadOnlyList<(int Id, string Text)> All = new (int, string)[]
    {
        (1, "¿Cómo se llamaba tu primera mascota?"),
        (2, "¿Cuál es tu ciudad natal?"),
        (3, "¿Cuál es el apellido de soltera de tu madre?"),
        (4, "¿Quién fue tu maestro o maestra favorito(a) de la infancia?"),
        (5, "¿Cuál es el nombre de tu mejor amigo de la infancia?"),
        (6, "¿En qué calle o avenida creciste?")
    };

    public static IReadOnlyList<SecurityQuestionCatalogItem> Catalog() =>
        All.Select(q => new SecurityQuestionCatalogItem(q.Id, q.Text)).ToList();

    public static bool IsValid(int questionId) => All.Any(q => q.Id == questionId);

    public static string Text(int questionId)
    {
        foreach (var q in All)
        {
            if (q.Id == questionId)
                return q.Text;
        }
        return string.Empty;
    }

    public static string NormalizeAnswer(string? answer) =>
        string.Join(
            ' ',
            (answer ?? string.Empty).Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
