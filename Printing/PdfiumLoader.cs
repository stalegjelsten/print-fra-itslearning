using System.Reflection;
using System.Runtime.InteropServices;

namespace PrintFraItslearning.Printing;

internal static class PdfiumLoader
{
    private const string ResourceName = "PrintFraItslearning.Native.pdfium.dll";
    private static int _initialized;
    private static string? _extractedPath;

    public static void EnsureLoaded()
    {
        if (Interlocked.Exchange(ref _initialized, 1) != 0) return;

        _extractedPath = ExtractPdfiumDll();

        // Pre-load DLL-en med full path slik at OS-cache vil tilfredsstille
        // senere DllImport("pdfium.dll")-kall.
        var handle = LoadLibraryW(_extractedPath);
        if (handle == IntPtr.Zero)
        {
            var err = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"Kunne ikke laste pdfium.dll fra '{_extractedPath}' (Win32-feilkode {err}). " +
                "Mangler kanskje Microsoft Visual C++ 2015-2022 Redistributable.");
        }

        // Registrer også en DllImportResolver for PdfiumViewer-assembly slik at
        // den unngår å lete i x86/x64-undermapper.
        try
        {
            var pdfiumViewerAsm = typeof(PdfiumViewer.PdfDocument).Assembly;
            NativeLibrary.SetDllImportResolver(pdfiumViewerAsm, Resolve);
        }
        catch
        {
            // Best-innsats — pre-loaded DLL skal være nok i de fleste tilfeller
        }
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName.Equals("pdfium.dll", StringComparison.OrdinalIgnoreCase) ||
            libraryName.Equals("pdfium", StringComparison.OrdinalIgnoreCase))
        {
            if (_extractedPath != null && NativeLibrary.TryLoad(_extractedPath, out var h))
                return h;
        }
        return IntPtr.Zero;
    }

    private static string ExtractPdfiumDll()
    {
        var nativeDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PrintFraItslearning", "native", "2.0.0");
        Directory.CreateDirectory(nativeDir);

        var target = Path.Combine(nativeDir, "pdfium.dll");

        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourceName}' ikke funnet i assemblyen.");

        // Skriv kun hvis filen mangler eller har feil størrelse
        if (!File.Exists(target) || new FileInfo(target).Length != stream.Length)
        {
            try
            {
                using var file = File.Create(target);
                stream.CopyTo(file);
            }
            catch (IOException)
            {
                // Filen kan være i bruk av en annen prosess — bruk eksisterende
                if (!File.Exists(target)) throw;
            }
        }

        return target;
    }

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibraryW(string lpLibFileName);
}
