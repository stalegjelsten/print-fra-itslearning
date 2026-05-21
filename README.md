# Print fra itslearning

Skriv ut alle elevbesvarelser fra itslearning på én gang. Programmet pakker ut zip-fila fra itslearning (eller leser en mappe direkte), finner alle Word-, PDF-, Excel-, HTML- og bildefiler, og skriver dem ut til en valgt printer — med elevens navn i toppteksten og sidenummer i bunnteksten.

Det er bygd som en vanlig Windows-app i C# / WinForms, distribuert som **én `.exe`-fil** uten installer eller administratorrettigheter.

## Funksjoner

- **Velg kilde**: ZIP-fil fra itslearning eller en mappe.
- **Filtyper**: Word (`.docx`), PDF, Excel (`.xlsx`, `.xlsm`, `.xls`), HTML, bilder og tekstfiler.
- **Topptekst og bunntekst** med mappenavnet (elevens navn) og «Side X av Y» — også på PDF og Excel.
- **Excel-utskrift** i liggende format med rutenett og rad-/kolonneoverskrifter. Valgfri formelvisning som ekstra ark.
- **Kombiner alt til én PDF** — én utskriftsjobb for hele klassen. Tomme sider mellom elever ved tosidig utskrift, slik at hver elev starter på et nytt ark.
- **Lagre kombinert PDF** til disk i stedet for (eller i tillegg til) å skrive ut.
- **Printer-velger** med liste over alle installerte printere.
- **Auto-update**: sjekker GitHub Releases ved oppstart og viser banner hvis nyere versjon finnes.
- **Ingen administratorrettigheter** nødvendig — kjører som vanlig bruker, konfig i `%APPDATA%`.

## Last ned

Hent siste versjon fra [Releases](https://github.com/stalegjelsten/print-fra-itslearning/releases). Last ned `PrintFraItslearning.exe`, legg den et sted du finner igjen (Skrivebord eller Nedlastinger), og dobbeltklikk for å starte.

**Første gang:** Windows kan vise en SmartScreen-advarsel fordi exe-en ikke er signert. Klikk **Mer info** → **Kjør likevel**. Dette kreves bare første gang.

## Bruk

1. Last ned besvarelsene fra itslearning som en zip-fil.
2. Start `PrintFraItslearning.exe`.
3. Klikk **Velg ZIP-fil fra itslearning** (eller **Velg mappe**).
4. Velg printer fra dropdown-en hvis den ikke allerede er riktig satt.
5. I neste vindu, huk av filene du vil skrive ut og velg innstillinger:
   - Topptekst og bunntekst
   - Word-kommentarer
   - Excel formelvisning
   - Kombiner til én PDF / tosidig utskrift / lagre som fil
   - Sorter etter fornavn
6. Klikk **Start →**.

## Krav på maskinen

- **Windows 10 eller nyere** (programmet er bygd som single-file self-contained, så `.NET 8 Runtime` trenger *ikke* være installert).
- **Microsoft Word** — for utskrift av Word- og HTML-filer.
- **Microsoft Excel** — for utskrift av Excel-filer.
- **Visual C++ Redistributable 2015-2022** — vanligvis allerede installert; brukes av den innebygde PDF-motoren.

## Konfigurasjon

Programmet leser/skriver `%APPDATA%\PrintFraItslearning\config.ini`:

```ini
printer=\\TDCSPRN30\Sikker_UtskriftCS
margin_cm=2.0
image_width_cm=17.0
```

Verdier kan endres direkte i fila, eller via «Lagre»-knappen i UI-en.

## Bygge fra kilde

Krever .NET 8 SDK (eller nyere).

```bash
# Restore + bygg
dotnet build -c Release

# Single-file self-contained Windows exe (~180 MB)
dotnet publish -c Release -r win-x64 \
  -p:PublishSingleFile=true \
  -p:SelfContained=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Output: `bin/Release/net8.0-windows/win-x64/publish/PrintFraItslearning.exe`.

Krysskompilering fra macOS/Linux fungerer fint takket være `EnableWindowsTargeting=true`.

## Arkitektur

```
PrintFraItslearning/
├── Program.cs                    # entry, ApplicationConfiguration.Initialize()
├── Config.cs                     # %APPDATA%\…\config.ini
├── PrintJob.cs                   # job-modell mellom Forms
├── AppIcon.cs                    # innebygd ikon
├── UpdateChecker.cs              # GitHub Releases poll
├── ZipExtractor.cs               # ZIP → %TEMP% + auto-cleanup
├── Assets/app.ico                # multi-size programikon (16…256 px)
├── Forms/
│   ├── SourceForm.cs             # ZIP/mappe + printer-dropdown
│   ├── SelectionForm.cs          # fil-tre + innstillinger
│   └── ProgressForm.cs           # framdrift + logg
├── Printing/
│   ├── WordPrinter.cs            # COM late binding (print + ExportToPdf)
│   ├── ExcelPrinter.cs           # Excel → PDF (landscape, gridlines, formelvisning)
│   ├── PdfPrinter.cs             # PdfiumViewer → PrintDocument
│   ├── PdfStamper.cs             # PDFsharp header/footer
│   ├── PdfMerger.cs              # PDFsharp page import + tom-side-padding
│   ├── PdfiumLoader.cs           # embed + LoadLibrary av pdfium.dll
│   ├── HtmlCombiner.cs           # bilder/txt/html → kombinert HTML
│   └── PrintQueueWatcher.cs      # LocalPrintServer-polling
└── Scanning/
    ├── FileScanner.cs            # rekursiv skann m/filtypegruppering
    └── StudentName.cs            # "Etternavn, Fornavn (epost)"-parser
```

## Forhistorie

Dette er andre generasjon av verktøyet. Første versjon var et PowerShell-skript ([print-word-files](https://github.com/stalegjelsten/print-word-files)) som sluttet å fungere på enkelte skole-PC-er da IT strammet til sikkerhetspolicyene (Constrained Language Mode, blokkering av `Add-Type`). Omskrivningen til C# fjerner hele angrepsoverflaten — det er bare en vanlig kompilert Windows-app uten dynamisk kodegenerering.

## Lisens

[MIT](LICENSE) — fri bruk, endring og videredistribusjon. Leveres «som det er», uten garantier.

## Forfatter

Utviklet av [Ståle Gjelsten](https://github.com/stalegjelsten) i samarbeid med språkmodellen [Claude](https://www.anthropic.com/claude) fra Anthropic.
