using System.Text.RegularExpressions;

namespace PrintFraItslearning.Scanning;

public sealed class StudentName
{
    private static readonly Regex Pattern =
        new(@"^(?<etternavn>[^,]+),\s+(?<fornavnRaw>[^(]+?)\s*\((?<epost>[^)]+)\)\s*$",
            RegexOptions.Compiled);

    public string Etternavn { get; init; } = "";
    public string Fornavn { get; init; } = "";
    public string AlleFornavnRaw { get; init; } = "";
    public string Epost { get; init; } = "";

    public static StudentName? TryParse(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName)) return null;
        var m = Pattern.Match(folderName);
        if (!m.Success) return null;
        var fornavnRaw = m.Groups["fornavnRaw"].Value.Trim();
        var fornavn = fornavnRaw.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0];
        return new StudentName
        {
            Etternavn = m.Groups["etternavn"].Value.Trim(),
            Fornavn = fornavn,
            AlleFornavnRaw = fornavnRaw,
            Epost = m.Groups["epost"].Value.Trim()
        };
    }
}
