
#set document(
  title: "Print fra itslearning",
  author: "Ståle Gjelsten",
)

#set page(
  paper: "a4",
  margin: (x: 2.5cm, y: 2.5cm),
  numbering: "1",
)

#set text(
  lang: "nb",
  size: 11pt,
)

#let kbd(body) = text(font: "Libertinus Keyboard")[#body]

#show raw.where(block: false): it => box(
  it,
  fill: black.lighten(95%),
  inset: (x: 2pt, y: 0pt),
  outset: (y: 3pt),
  radius: 2pt,
)

#show raw.where(block: true): it => block(
  it,
  fill: black.lighten(95%),
  inset: 8pt,
  radius: 2pt,
  width: 100%,
)


#set par(justify: true)

#show link: it => {
  underline(stroke: (paint: red.darken(80%), thickness: 1pt, dash: "dotted"), offset: 0.15em)[#text(fill: red.darken(80%))[#it]]
  if type(it.dest) == type("hello") {
    sym.wj
    h(1.0pt)
    sym.wj
    super(box(height: 0.05em, text(size: 0.7em, fill: red.darken(80%))[#sym.arrow.tr]))
  }
}




#align(center)[
  #text(size: 20pt, weight: "bold")[
    Print fra itslearning
  ]

  #text(size: 14pt)[
    Skriv ut alle elevbesvarelser automatisk
  ]

  #v(0.5cm)

  #text(size: 12pt)[
    Ståle Gjelsten \
    Dahlske videregående skole
  ]
  #v(1cm)
]

= Hensikt

Dette programmet er laget for å gjøre det enkelt for lærere å skrive ut alle elevbesvarelsene fra itslearning på én gang. I stedet for å åpne og skrive ut hver besvarelse manuelt, kan du bruke dette programmet til å skrive ut alt automatisk -- gjerne med elevens navn i toppteksten og sidetall i bunnteksten på dokumentene.

Programmet er en helt vanlig Windows-app som distribueres som én `.exe`-fil. Det krever ingen installer og ingen administratorrettigheter.

= Hva programmet gjør

Programmet skriver ut alle dokumenter og bilder i en mappe (eller zip-fil fra itslearning) og alle undermapper automatisk. Programmet håndterer hver filtype slik:

== Word-filer (.docx)
Word-dokumenter skrives ut som de er skrevet. Du kan velge å legge elevens navn i toppteksten og sidenummer i bunnteksten (format: "Side 1 av 5"), og du kan velge om kommentarer/endringsmerker skal være med.

== Excel-filer (.xlsx, .xlsm, .xls)
Excel-filer skrives ut i liggende format med rutenett og rad-/kolonneoverskrifter (A, B, C... og 1, 2, 3...). Du kan velge å skrive ut to varianter av hver fil: én med beregnede verdier og én med formlene synlige.

== PDF-filer (.pdf)
PDF-filer skrives ut direkte uten å åpne Adobe Reader. Hvis du har valgt topptekst og bunntekst, blir disse stemplet inn på en midlertidig kopi av PDF-en før utskriften, slik at elevens navn også vises på PDF-utskrifter.

== HTML-filer, bilder og tekstfiler
For hver elev kombineres alle HTML-filer (.html, .htm), bilder (.jpg, .jpeg, .png, .gif, .bmp) og tekstfiler (.txt) til én midlertidig HTML-fil. Denne skrives ut via Word. Den midlertidige filen slettes automatisk etter utskrift.

#block(
  fill: blue.lighten(79%),
  inset: 1em,
  radius: 0.3em,
)[
  *Viktig:* Originaldokumentene endres IKKE. Topptekst og bunntekst stemples inn på en midlertidig kopi som slettes etter utskrift.
]

= Hvordan bruke programmet

== Steg 0: Last ned programmet

Last ned `PrintFraItslearning.exe` fra
#link("https://github.com/stalegjelsten/print-fra-itslearning/releases")[*Releases-siden på GitHub*]
og legg den et sted du finner den igjen (f.eks. Skrivebord eller Nedlastinger). Du trenger ikke å installere noe -- bare dobbeltklikk på fila for å starte.

#block(
  fill: blue.lighten(79%),
  inset: 1em,
  radius: 0.3em,
)[
  *Første gang du kjører fila:* Windows kan vise en advarsel om at programmet «ikke er gjenkjent» (SmartScreen). Klikk *Mer info* og deretter *Kjør likevel*. Denne advarselen kommer bare første gang.
]

== Steg 1: Last ned besvarelser fra itslearning

Logg inn på itslearning og gå til oppgaven du vil skrive ut besvarelser fra.

#figure(
  image("assets/itslearning-download-answers.png", width: 60%),
  caption: [Nedlasting av besvarelser fra itslearning]
)<itslearning>

+ Vis kun elevene som har levert oppgaven ved å velge *Vis:* *Levert*. Se @itslearning.
+ *Merk alle elevene* du vil skrive ut besvarelser for (huk av øverst for å velge alle).
+ Klikk på *Handlinger*.
+ *"Last ned besvarelser"*.
+ Itslearning bruker litt tid på å samle alle besvarelsene til én fil. Trykk på *klikk her for å laste ned* når filene er klare.
+ En zip-fil#footnote[En zip-fil er en mappe som er pakket sammen til én enkelt fil, slik at det er enkelt å laste den ned og flytte den.] lastes ned til datamaskinen din (vanligvis i Nedlastinger-mappen).

== Steg 2: Kjør utskriftsprogrammet

+ Dobbeltklikk på `PrintFraItslearning.exe`.
+ I det første vinduet klikker du på enten *Velg ZIP-fil fra itslearning* eller *Velg mappe*, og velger fila/mappa. (Du kan også klikke *Avslutt* for å lukke programmet.)
+ Programmet skanner filene og åpner et nytt vindu hvor du ser alle filene gruppert etter type, med en forhåndsvisning til høyre.
+ I dette vinduet kan du:
  - Velge *printer* fra nedtrekkslisten øverst (programmet henter automatisk alle installerte printere). Valget huskes til neste gang. Klikk på ↻-knappen for å oppdatere listen.
  - *Markere en fil* i lista for å se forhåndsvisning til høyre. PDF, bilder, HTML og tekstfiler vises automatisk. Word- og Excel-filer kan ikke forhåndsvises i programmet -- *dobbeltklikk* for å åpne dem i Word/Excel i stedet.
  - *Avhuke* enkeltfiler du *ikke* vil skrive ut.
  - Slå av/på *topptekst og bunntekst* (mappenavn + sidenummer).
  - Slå av/på utskrift av *kommentarer* i Word-dokumenter.
  - Slå av/på *formelvisning* for Excel-filer.
  - Velge *Kombiner alle filer til én PDF og skriv ut som én jobb* (se under).
  - Velge *Sorter etter fornavn* i stedet for etternavn (kun synlig hvis mappenavnene har formatet «Etternavn, Fornavn (epost)»).
+ Klikk *Start →* for å begynne. Programmet viser fremgang i et eget vindu med en logg som viser hver fil etter hvert som den skrives ut.

== Kombinert PDF-modus

Hvis du huker av *Kombiner alle filer til én PDF og skriv ut som én jobb* får du tre tilleggsvalg:

- *Tosidig utskrift: legg inn tomme sider mellom elever* -- sikrer at hver elev alltid starter på forsiden av et nytt ark når printeren skriver ut tosidig. Anbefales hvis du printer duplex.
- *Send kombinert PDF til skriveren* -- standard. Skriver ut alt som én jobb.
- *Lagre kombinert PDF som fil* -- åpner en lagre-dialog hvor du velger hvor PDF-en skal lagres. Du kan både skrive ut og lagre, eller bare gjøre én av delene.

Denne modusen er nyttig når du vil:

- Arkivere besvarelsene som én PDF.
- Få én utskriftsjobb i printerkøen i stedet for én jobb per fil.
- Forhåndsvise den ferdige utskriften før du sender den.

= Krav

Programmet fungerer kun på Windows 10 eller nyere, og det krever:

- *Microsoft Word* -- for utskrift av Word- og HTML-filer.
- *Microsoft Excel* -- for utskrift av Excel-filer.
- *Visual C++ 2015-2022 Redistributable* -- vanligvis allerede installert; brukes av den innebygde PDF-motoren.

PDF-utskrift krever *ikke* lenger Adobe Reader -- PDF-motoren er innebygd i programmet.

= Endre innstillinger

Programmet lagrer innstillinger i `%APPDATA%\PrintFraItslearning\config.ini`. Du finner mappa ved å lime inn `%APPDATA%\PrintFraItslearning` i adressefeltet i Filutforsker.

Innholdet i fila ser slik ut:

```ini
printer=\\TDCSPRN30\Sikker_UtskriftCS
margin_cm=2.0
image_width_cm=17.0
```

- *printer*: hvilken printer programmet sender utskriftene til. Kan også endres via dropdown-en i programmet.
- *margin_cm*: sidemarger i centimeter (standard: 2.0 cm).
- *image_width_cm*: maksimal bildebredde i centimeter (standard: 17.0 cm).

= Feilsøking

== Hvis Windows blokkerer programmet

Første gang du kjører fila kan Windows vise en advarsel om at programmet «ikke er gjenkjent». Dette skyldes at exe-en ikke er digitalt signert. Klikk *Mer info* og deretter *Kjør likevel*.

Hvis dette ikke fungerer (IT-policy kan blokkere usignerte programmer), kan du prøve å høyreklikke på exe-en → *Egenskaper* → huk av *«Fjern blokkering»* nederst → klikk *OK*.

== Hvis Word- eller HTML-filer ikke skrives ut

Kontroller at Microsoft Word er installert på datamaskinen.

== Hvis Excel-filer ikke skrives ut

Kontroller at Microsoft Excel er installert på datamaskinen.

== Hvis PDF-filer gir feilmelding om manglende DLL

PDF-motoren krever Visual C++ 2015-2022 Redistributable. Last ned og installer #link("https://aka.ms/vs/17/release/vc_redist.x64.exe")[Microsoft Visual C++ Redistributable (x64)] fra Microsoft.

== Hvis printer-listen er tom

Klikk på den runde pilen (↻) ved siden av printer-dropdown-en (øverst i utvalg-vinduet) for å hente listen på nytt.

== Hvis forhåndsvisningen ikke vises

PDF-er, bilder, HTML og tekstfiler får forhåndsvisning automatisk. Word- og Excel-filer er ikke støttet i forhåndsvisningen -- dobbeltklikk på fila i lista for å åpne den i Word eller Excel i stedet.

= Kontakt og lisens

Programmet er utviklet av #link("https://github.com/stalegjelsten")[Ståle Gjelsten] i samarbeid med språkmodellen #link("https://www.anthropic.com/claude")[Claude] fra Anthropic.

Kildekoden ligger på #link("https://github.com/stalegjelsten/print-fra-itslearning")[GitHub], og programmet er lisensiert under #link("https://opensource.org/license/MIT")[MIT-lisens] -- det kan fritt brukes, endres og deles videre. Programmet leveres «som det er», uten noen form for garantier.
