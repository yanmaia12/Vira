# Vira — File Converter

Vira is a Windows desktop application for converting Office documents, PDFs, and images. It adds conversion actions to File Explorer's right-click menu and lets you draw a signature on your phone, send it to your PC with a QR code, and place it on a selected PDF page.

## Requirements and technologies

- Windows 10 or 11 and the .NET 8 SDK for development.
- WPF for the desktop interface.
- Microsoft Office COM automation for Word, Excel, and PowerPoint conversions. The corresponding Office applications must be installed.
- Windows Imaging Component (WIC), through WPF encoders and decoders, for image conversion.
- PDFiumRaster 2.0.5 (MIT license) to render selected PDF pages for preview.
- PDFsharp 6.2.4 (MIT license) to add the signature image to a PDF.
- QRCoder 1.8.0 to generate QR codes.
- ASP.NET Core/Kestrel, included in .NET, to serve the mobile signing page over the local network.
- Per-user File Explorer menu registration (`HKCU`); administrator privileges are not required.

## Run from source

From the repository directory:

```powershell
dotnet restore ConversorFicheiros.sln
dotnet run --project ConversorFicheiros/ConversorFicheiros.csproj
```

## Install for everyday use

To build a per-user installer on Windows (no administrator privileges required, and end users do not need to install .NET), run:

```powershell
powershell -ExecutionPolicy Bypass -File installer/Build-Installer.ps1
```

The generated `Vira-Setup.exe` in the repository root installs Vira to `%LOCALAPPDATA%\Programs\Vira`, creates Start Menu and Desktop shortcuts, and registers the File Explorer context-menu commands for the current user. After installation, open **Vira** from either shortcut; you do not need to run `dotnet restore` or `dotnet run` again. The installer publishes a self-contained Windows x64 application. Office conversions still require Microsoft Office to be installed.

To publish the application directly as a self-contained Windows x64 executable:

```powershell
dotnet publish ConversorFicheiros/ConversorFicheiros.csproj -c Release -r win-x64 --self-contained true
```

Run the automated tests with:

```powershell
dotnet test ConversorFicheiros.sln
```

## Features

- Add multiple files with the file picker or by dragging them into the window.
- Convert Word files (`DOC`, `DOCX`, `RTF`, `TXT`, `HTML`, `ODT`) to PDF and other Word formats supported by the installed Office version.
- Convert PDFs to DOCX or TXT by importing them into Word. Pagination and layout may change during conversion.
- Convert Excel files (`XLS`, `XLSX`, `XLSM`, `CSV`, `ODS`) to PDF, XLSX, XLS, or CSV; convert PowerPoint files (`PPT`, `PPTX`, `PPTM`, `ODP`) to PDF, PPTX, or PPT.
- Convert WIC-supported images to PNG, JPEG, BMP, TIFF, or PDF. Each image frame becomes a separate PDF page. Output files are saved next to the source files.
- Register per-user File Explorer context-menu commands for Office documents, PDFs, and images. On Windows 11, these commands may appear under **Show more options**.
- Select a PDF, scan its QR code with a phone on the same Wi-Fi network, draw a signature, choose a page, position and resize the signature in the preview, and save a `_assinado.pdf` copy.
- Avoid overwriting existing output files by adding a numeric suffix when needed.

## Signature and privacy

The drawn signature is an image embedded visually in the PDF. Before saving, choose a page and drag or resize the signature on that page. This is not a qualified electronic signature: it does not use a certificate and does not verify identity or document integrity. The mobile signing session is served over HTTP on the local network and uses a random token in its URL. The phone and PC must be able to communicate, and Windows Firewall may ask for permission to allow the application. The signed copy is created next to the original PDF.

## Current limitations

Document conversion requires Word, Excel, or PowerPoint and depends on the formats and filters supported by the installed Office version. Vira does not cover every format, and it does not convert PDFs to Excel or PowerPoint or handle multimedia formats. Each signing operation applies the signature to one page. Protected PDFs and rotated pages are not handled specially. File Explorer commands point to the executable that registered them, so register the commands again after moving the application.

## Architecture and roadmap

See [docs/architecture.md](docs/architecture.md) for architecture decisions, components, and implementation milestones. Add new format support through independent conversion providers so the interface does not depend on the details of a particular library.
