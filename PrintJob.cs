using System.IO;
using PrintFraItslearning.Scanning;

namespace PrintFraItslearning;

public enum PrintSortMode
{
    LastNameFromFolder,
    FirstNameFromFolder,
    PrintedFileName
}

public sealed class PrintJob
{
    public required Config Config { get; init; }
    public required string Printer { get; init; }
    public required List<ScannedFile> Files { get; init; }
    public required bool AddHeaderFooter { get; init; }
    public required bool PrintWordComments { get; init; }
    public required bool PrintExcelFormulas { get; init; }
    public required PrintSortMode SortMode { get; init; }
    public required bool CombineToOnePdf { get; init; }
    public required bool DuplexPadBlankPages { get; init; }
    public required bool PrintCombinedPdf { get; init; }
    public required bool SaveCombinedPdf { get; init; }
    public string? SaveCombinedPdfPath { get; init; }
    public List<FileInfo> GeneratedHtmlFiles { get; init; } = new();
    public ZipExtractor? TempExtraction { get; init; }
}

public sealed record PrintResultRow(string Name, string Folder, bool Success, string Detail);
