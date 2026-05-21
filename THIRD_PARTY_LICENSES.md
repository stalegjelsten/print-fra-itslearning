# Tredjepartslisenser

Den distribuerte `PrintFraItslearning.exe` bundler følgende åpen kildekode-komponenter. Lisensene under er bevart i samsvar med disse komponentenes krav.

---

## PDFsharp

- Versjon: 6.1.1
- Bruk: PDF-manipulering og sammenslåing
- Lisens: MIT
- Copyright © Empira Software GmbH
- Prosjektside: <https://github.com/empira/PDFsharp>

---

## PdfiumViewer

- Versjon: 2.13.0
- Bruk: PDF-rendring og utskrift
- Lisens: Apache License 2.0
- Copyright © Pieter van Ginkel
- Prosjektside: <https://github.com/pvginkel/PdfiumViewer>

```
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
```

---

## PdfiumViewer.Native (PDFium-binærer)

- Versjon: 2018.4.8.256 (x86_64.v8-xfa)
- Bruk: native PDF-motor (`pdfium.dll`)
- Lisens: BSD 3-Clause (PDFium), med deler under Apache 2.0
- Copyright © 2014 The PDFium Authors. All rights reserved.
- Prosjektside: <https://pdfium.googlesource.com/pdfium/>

```
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

    * Redistributions of source code must retain the above copyright
notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above
copyright notice, this list of conditions and the following disclaimer
in the documentation and/or other materials provided with the
distribution.
    * Neither the name of Google Inc. nor the names of its
contributors may be used to endorse or promote products derived from
this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```

---

## .NET 8 Runtime og BCL

- Bruk: kjøretidsmiljø (bundlet via self-contained publish)
- Lisens: MIT
- Copyright © .NET Foundation and Contributors
- Prosjektside: <https://github.com/dotnet/runtime>

Inkluderer blant annet:

- `System.Management` 8.0.0 (MIT)
- `System.Windows.Forms` (MIT)
- `System.Drawing.Common` (MIT)
- `System.Printing` (MIT)

---

## Microsoft Office Interop

Programmet snakker med Microsoft Word og Microsoft Excel via COM på sluttbrukerens maskin. Office-installasjonen leveres ikke som en del av dette programmet, og brukeren må ha gyldig Office-lisens.
