using System.IO;

namespace ConversorFicheiros;

/// <summary>Maps known inputs to formats that an installed conversion provider can produce.</summary>
internal static class ConversionCatalog
{
    private static readonly string[] ImageInputs = [".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff", ".gif"];
    private static readonly string[] WordInputs = [".doc", ".docx", ".docm", ".rtf", ".txt", ".html", ".htm", ".odt", ".pdf"];
    private static readonly string[] SpreadsheetInputs = [".xls", ".xlsx", ".xlsm", ".csv", ".ods"];
    private static readonly string[] PresentationInputs = [".ppt", ".pptx", ".pptm", ".odp"];

    public static IReadOnlyList<string> TargetsFor(string inputPath)
    {
        var input = Path.GetExtension(inputPath).ToLowerInvariant();
        if (ImageInputs.Contains(input)) return ["pdf", "png", "jpg", "bmp", "tiff"];
        if (input == ".pdf") return ["docx", "txt"];
        if (WordInputs.Contains(input)) return RemoveSame(input, ["pdf", "docx", "doc", "rtf", "txt", "html"]);
        if (SpreadsheetInputs.Contains(input)) return RemoveSame(input, ["pdf", "xlsx", "xls", "csv"]);
        if (PresentationInputs.Contains(input)) return RemoveSame(input, ["pdf", "pptx", "ppt"]);
        return [];
    }

    public static IEnumerable<string> RegisteredInputs => ImageInputs.Concat(WordInputs).Concat(SpreadsheetInputs).Concat(PresentationInputs).Distinct();

    public static bool IsImage(string extension) => ImageInputs.Contains(extension, StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyList<string> RemoveSame(string extension, IEnumerable<string> formats) =>
        formats.Where(format => !string.Equals(extension, "." + format, StringComparison.OrdinalIgnoreCase)).ToArray();
}

internal sealed class FileConversionService
{
    private readonly ImageConversionService _images = new();

    public string Convert(string input, string targetFormat)
    {
        var target = targetFormat.Trim().TrimStart('.').ToLowerInvariant();
        if (!File.Exists(input)) throw new FileNotFoundException("O ficheiro de origem já não existe.", input);
        if (!ConversionCatalog.TargetsFor(input).Contains(target, StringComparer.OrdinalIgnoreCase))
            throw new NotSupportedException($"Não é possível converter {Path.GetExtension(input)} para .{target}.");

        if (ConversionCatalog.IsImage(Path.GetExtension(input))) return _images.Convert(input, target);
        return OfficeConversionService.Convert(input, target);
    }
}

/// <summary>
/// Uses the user's installed Microsoft Office applications for document layout and export.
/// COM automation is kept on a dedicated STA thread, as required by Office.
/// </summary>
internal static class OfficeConversionService
{
    public static string Convert(string input, string target)
    {
        string? result = null;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { result = ConvertOnSta(input, target); }
            catch (Exception exception) { failure = exception; }
        }) { IsBackground = true, Name = "Vira Office conversion" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) throw new InvalidOperationException($"A conversão do documento falhou: {failure.Message}", failure);
        return result ?? throw new InvalidOperationException("O Office não devolveu um ficheiro convertido.");
    }

    private static string ConvertOnSta(string input, string target)
    {
        var extension = Path.GetExtension(input).ToLowerInvariant();
        if (extension == ".pdf" || IsWord(extension)) return ConvertWord(input, target);
        if (IsSpreadsheet(extension)) return ConvertExcel(input, target);
        if (IsPresentation(extension)) return ConvertPowerPoint(input, target);
        throw new NotSupportedException($"Não existe um conversor de documentos para {extension}.");
    }

    private static string ConvertWord(string input, string target)
    {
        object? appObject = null;
        object? collectionObject = null;
        object? documentObject = null;
        try
        {
            appObject = CreateOfficeApplication("Word.Application", "Microsoft Word");
            dynamic app = appObject;
            app.Visible = false;
            app.DisplayAlerts = 0;
            TrySet(() => app.AutomationSecurity = 3);
            dynamic documents = app.Documents;
            collectionObject = documents;
            object missing = Type.Missing;
            dynamic document = documents.Open(input, false, true, false, missing, missing, false, missing, missing, missing, missing, false, false, missing, true, missing);
            documentObject = document;
            var output = OutputPath(input, target);
            if (target == "pdf") document.ExportAsFixedFormat(output, 17);
            else document.SaveAs2(output, WordFileFormat(target));
            document.Close(false);
            Release(documentObject);
            documentObject = null;
            return output;
        }
        finally { CloseOffice(appObject, collectionObject, documentObject, "Word"); }
    }

    private static string ConvertExcel(string input, string target)
    {
        object? appObject = null;
        object? collectionObject = null;
        object? documentObject = null;
        try
        {
            appObject = CreateOfficeApplication("Excel.Application", "Microsoft Excel");
            dynamic app = appObject;
            app.Visible = false;
            app.DisplayAlerts = false;
            TrySet(() => app.AutomationSecurity = 3);
            dynamic books = app.Workbooks;
            collectionObject = books;
            object missing = Type.Missing;
            dynamic workbook = books.Open(input, 0, true, missing, missing, missing, true, missing, missing, false, false, missing, false, true, missing);
            documentObject = workbook;
            var output = OutputPath(input, target);
            if (target == "pdf") workbook.ExportAsFixedFormat(0, output);
            else workbook.SaveAs(output, ExcelFileFormat(target));
            workbook.Close(false);
            Release(documentObject);
            documentObject = null;
            return output;
        }
        finally { CloseOffice(appObject, collectionObject, documentObject, "Excel"); }
    }

    private static string ConvertPowerPoint(string input, string target)
    {
        object? appObject = null;
        object? collectionObject = null;
        object? documentObject = null;
        try
        {
            appObject = CreateOfficeApplication("PowerPoint.Application", "Microsoft PowerPoint");
            dynamic app = appObject;
            app.DisplayAlerts = 1;
            TrySet(() => app.AutomationSecurity = 3);
            dynamic presentations = app.Presentations;
            collectionObject = presentations;
            dynamic presentation = presentations.Open(input, 0, -1, 0);
            documentObject = presentation;
            var output = OutputPath(input, target);
            if (target == "pdf") presentation.ExportAsFixedFormat(output, 2);
            else presentation.SaveAs(output, target == "ppt" ? 1 : 24);
            presentation.Close();
            Release(documentObject);
            documentObject = null;
            return output;
        }
        finally { CloseOffice(appObject, collectionObject, documentObject, "PowerPoint"); }
    }

    private static object CreateOfficeApplication(string progId, string productName)
    {
        var type = Type.GetTypeFromProgID(progId, throwOnError: false);
        if (type is null)
            throw new InvalidOperationException($"{productName} não está instalado. Instale o Office para converter este formato.");
        return Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Não foi possível iniciar {productName}.");
    }

    private static void CloseOffice(object? appObject, object? collectionObject, object? documentObject, string application)
    {
        if (documentObject is not null)
        {
            try { ((dynamic)documentObject).Close(false); } catch { }
            Release(documentObject);
        }
        Release(collectionObject);
        if (appObject is not null)
        {
            try { ((dynamic)appObject).Quit(); } catch { }
            Release(appObject);
        }
    }

    private static void Release(object? value)
    {
        if (value is null) return;
        try { if (System.Runtime.InteropServices.Marshal.IsComObject(value)) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(value); } catch { }
    }

    private static void TrySet(Action action) { try { action(); } catch { } }

    private static bool IsWord(string extension) => extension is ".doc" or ".docx" or ".docm" or ".rtf" or ".txt" or ".html" or ".htm" or ".odt";
    private static bool IsSpreadsheet(string extension) => extension is ".xls" or ".xlsx" or ".xlsm" or ".csv" or ".ods";
    private static bool IsPresentation(string extension) => extension is ".ppt" or ".pptx" or ".pptm" or ".odp";

    private static int WordFileFormat(string target) => target switch
    {
        "doc" => 0, "rtf" => 6, "txt" => 2, "html" => 8, _ => 16
    };

    private static int ExcelFileFormat(string target) => target switch
    {
        "xls" => 56, "csv" => 62, _ => 51
    };

    private static string OutputPath(string input, string target)
    {
        var extension = "." + target;
        var stem = Path.GetFileNameWithoutExtension(input);
        if (string.Equals(Path.GetExtension(input), extension, StringComparison.OrdinalIgnoreCase)) stem += "_convertido";
        var directory = Path.GetDirectoryName(input)!;
        var path = Path.Combine(directory, stem + extension);
        var suffix = 2;
        while (File.Exists(path)) path = Path.Combine(directory, $"{stem} ({suffix++}){extension}");
        return path;
    }
}
