using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using PdfiumRaster;
using QRCoder;

namespace ConversorFicheiros;

public partial class MainWindow : Window
{
    private readonly FileConversionService _converter = new();
    private MobileSignatureServer? _server;

    public MainWindow()
    {
        InitializeComponent();
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        if (args.Length >= 3 && args[0] == "--convert")
        {
            AddFiles([args[1]]);
            TargetFormat.SelectedItem = TargetFormat.Items.Cast<System.Windows.Controls.ComboBoxItem>()
                .FirstOrDefault(item => string.Equals(item.Content?.ToString()?.TrimStart('.'), args[2].TrimStart('.'), StringComparison.OrdinalIgnoreCase));
            Dispatcher.BeginInvoke(async () => await ConvertFilesAsync());
        }
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Selecionar ficheiros", Multiselect = true, Filter = "Todos os ficheiros|*.*" };
        if (dialog.ShowDialog() == true) AddFiles(dialog.FileNames);
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths.Where(File.Exists)) if (!FileList.Items.Contains(path)) FileList.Items.Add(path);
        UpdateTargetFormats();
        StatusText.Text = $"{FileList.Items.Count} ficheiro(s) na lista";
    }

    private void UpdateTargetFormats()
    {
        var selected = (TargetFormat.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
        var files = FileList.Items.Cast<string>().ToArray();
        var targets = files.Length == 0
            ? Array.Empty<string>()
            : files.Select(ConversionCatalog.TargetsFor).Aggregate((left, right) => left.Intersect(right, StringComparer.OrdinalIgnoreCase).ToArray());
        TargetFormat.Items.Clear();
        foreach (var target in targets)
            TargetFormat.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = $".{target}" });
        TargetFormat.SelectedItem = TargetFormat.Items.Cast<System.Windows.Controls.ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Content?.ToString(), selected, StringComparison.OrdinalIgnoreCase))
            ?? TargetFormat.Items.Cast<System.Windows.Controls.ComboBoxItem>().FirstOrDefault();
        ConvertButton.IsEnabled = TargetFormat.SelectedItem is not null;
    }

    private void DropTarget_DragOver(object sender, DragEventArgs e) => e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
    private void DropTarget_Drop(object sender, DragEventArgs e) { if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) AddFiles(paths); }
    private async void Convert_Click(object sender, RoutedEventArgs e) => await ConvertFilesAsync();

    private async Task ConvertFilesAsync()
    {
        if (FileList.Items.Count == 0) { StatusText.Text = "Adicione ficheiros para converter."; return; }
        var format = (TargetFormat.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
        if (format is null) { StatusText.Text = "Não há um formato de destino em comum para os ficheiros escolhidos."; return; }
        ConvertButton.IsEnabled = false;
        var successes = 0; var total = FileList.Items.Count;
        foreach (string input in FileList.Items)
        {
            try { var output = await Task.Run(() => _converter.Convert(input, format)); successes++; StatusText.Text = $"Convertido: {Path.GetFileName(output)}"; }
            catch (Exception ex) { StatusText.Text = $"Falha em {Path.GetFileName(input)}: {ex.Message}"; }
        }
        ConvertButton.IsEnabled = true;
        if (successes > 0) StatusText.Text = $"Concluído: {successes} de {total} ficheiro(s).";
    }

    private void Clear_Click(object sender, RoutedEventArgs e) { FileList.Items.Clear(); UpdateTargetFormats(); StatusText.Text = "Lista limpa."; }

    private void ContextMenu_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var exe = Environment.ProcessPath ?? throw new InvalidOperationException("Não foi possível localizar o executável.");
            new ExplorerMenuService().Register(exe);
            StatusText.Text = "Atalhos de documentos e imagens registados no Explorador. No Windows 11, procure-os em Mostrar mais opções.";
        }
        catch (Exception ex) { StatusText.Text = $"Não foi possível registar o menu: {ex.Message}"; }
    }

    private async void SignPdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Selecionar PDF", Filter = "Documentos PDF|*.pdf" };
        if (dialog.ShowDialog() != true) return;
        await StopSignatureServerAsync();
        _server = new MobileSignatureServer();
        try
        {
            var session = await _server.StartAsync();
            QrImage.Source = ToBitmap(PngByteQRCodeHelper.GetQRCode(session.Url, QRCodeGenerator.ECCLevel.Q, 8));
            QrUrlText.Text = session.Url;
            QrPanel.Visibility = Visibility.Visible;
            StatusText.Text = "Leia o QR code na mesma rede Wi-Fi e desenhe a assinatura no telemóvel.";
            var png = await session.SignatureTask;
            await StopSignatureServerAsync();
            QrPanel.Visibility = Visibility.Collapsed;
            StatusText.Text = "Assinatura recebida. A preparar a pré-visualização…";
            var preview = await Task.Run(() => PdfPreviewRenderer.RenderFirstPage(dialog.FileName));
            var placementWindow = new SignaturePlacementWindow(ToBitmap(preview.Png), png, preview.PageWidthPoints, preview.PageHeightPoints) { Owner = this };
            if (placementWindow.ShowDialog() != true || placementWindow.Placement is null)
            {
                StatusText.Text = "Operação cancelada. O PDF original foi preservado.";
                return;
            }

            StatusText.Text = "A aplicar a assinatura e a guardar o PDF…";
            var output = Path.Combine(Path.GetDirectoryName(dialog.FileName)!, Path.GetFileNameWithoutExtension(dialog.FileName) + "_assinado.pdf");
            output = await Task.Run(() => PdfSignatureService.AddVisualSignature(dialog.FileName, output, png, placementWindow.Placement));
            StatusText.Text = $"PDF assinado guardado em {output}";
            MessageBox.Show(this, $"A assinatura foi aplicada com sucesso.\n\nFicheiro guardado em:\n{output}", "PDF assinado", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Erro na assinatura: {ex.Message}";
            MessageBox.Show(this, $"Não foi possível concluir a assinatura.\n\n{ex.Message}", "Erro ao assinar PDF", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            QrPanel.Visibility = Visibility.Collapsed;
            await StopSignatureServerAsync();
        }
    }

    private async Task StopSignatureServerAsync()
    {
        var server = _server;
        _server = null;
        if (server is not null) await server.DisposeAsync();
    }

    private static BitmapImage ToBitmap(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze(); return bitmap;
    }

    protected override void OnClosed(EventArgs e)
    {
        var server = _server;
        _server = null;
        if (server is not null) _ = server.DisposeAsync();
        base.OnClosed(e);
    }
}

internal sealed record PdfPagePreview(byte[] Png, double PageWidthPoints, double PageHeightPoints);

internal static class PdfPreviewRenderer
{
    public static PdfPagePreview RenderFirstPage(string pdfPath)
    {
        var pageSizes = PdfImageConverter.GetPageSizes(pdfPath);
        if (pageSizes.Count == 0)
            throw new InvalidDataException("Não foi possível ler a primeira página deste PDF.");
        var pageSize = pageSizes[0];
        if (pageSize.Width <= 0 || pageSize.Height <= 0)
            throw new InvalidDataException("As dimensões da primeira página deste PDF são inválidas.");

        using var session = PdfRenderSession.Open(pdfPath);
        using var image = new MemoryStream();
        session.SavePage(0, image, new PdfImageConversionOptions
        {
            Format = PdfImageOutputFormat.Png,
            Encoding = PdfImageEncodingOptions.Fast,
            Render = PdfPageRenderOptions.ScreenPreview
        });
        return new PdfPagePreview(image.ToArray(), pageSize.Width, pageSize.Height);
    }
}

internal sealed class ImageConversionService
{
    public string Convert(string input, string format)
    {
        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase)) return ConvertToPdf(input);
        var (encoder, extension) = format.ToLowerInvariant() switch
        {
            "png" => ((BitmapEncoder)new PngBitmapEncoder(), ".png"),
            "jpg" or "jpeg" => (new JpegBitmapEncoder { QualityLevel = 92 }, ".jpg"),
            "bmp" => (new BmpBitmapEncoder(), ".bmp"),
            "tiff" => (new TiffBitmapEncoder(), ".tiff"),
            _ => throw new NotSupportedException($"Formato não suportado: {format}")
        };
        var decoder = BitmapDecoder.Create(new Uri(input), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        foreach (var frame in decoder.Frames) encoder.Frames.Add(frame);
        var directory = Path.GetDirectoryName(input)!;
        var output = Path.Combine(directory, Path.GetFileNameWithoutExtension(input) + extension);
        if (string.Equals(Path.GetFullPath(input), Path.GetFullPath(output), StringComparison.OrdinalIgnoreCase))
            output = Path.Combine(directory, Path.GetFileNameWithoutExtension(input) + "_convertido" + extension);
        output = GetAvailablePath(output);
        using var stream = File.Create(output); encoder.Save(stream); return output;
    }

    private static string ConvertToPdf(string input)
    {
        var decoder = BitmapDecoder.Create(new Uri(input), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var output = GetAvailablePath(Path.Combine(Path.GetDirectoryName(input)!, Path.GetFileNameWithoutExtension(input) + ".pdf"));
        using var document = new PdfSharp.Pdf.PdfDocument();
        foreach (var frame in decoder.Frames)
        {
            var pngEncoder = new PngBitmapEncoder(); pngEncoder.Frames.Add(frame);
            using var imageStream = new MemoryStream(); pngEncoder.Save(imageStream); imageStream.Position = 0;
            using var image = XImage.FromStream(imageStream);
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(frame.PixelWidth * 72d / frame.DpiX);
            page.Height = XUnit.FromPoint(frame.PixelHeight * 72d / frame.DpiY);
            using var graphics = XGraphics.FromPdfPage(page);
            graphics.DrawImage(image, 0, 0, page.Width.Point, page.Height.Point);
        }
        document.Save(output);
        return output;
    }

    private static string GetAvailablePath(string path)
    {
        var candidate = path; var suffix = 2;
        while (File.Exists(candidate)) candidate = Path.Combine(Path.GetDirectoryName(path)!, $"{Path.GetFileNameWithoutExtension(path)} ({suffix++}){Path.GetExtension(path)}");
        return candidate;
    }
}

internal sealed class ExplorerMenuService
{
    public void Register(string executable)
    {
        if (!File.Exists(executable)) throw new FileNotFoundException("Executável da aplicação não encontrado.", executable);
        foreach (var ext in ConversionCatalog.RegisteredInputs)
        foreach (var format in ConversionCatalog.TargetsFor("ficheiro" + ext))
        {
            using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\SystemFileAssociations\{ext}\shell\ConversorFicheiros.{format}");
            key!.SetValue(null, $"Converter para {format.ToUpperInvariant()}"); key.SetValue("Icon", executable);
            using var command = key.CreateSubKey("command"); command!.SetValue(null, $"\"{executable}\" --convert \"%1\" {format}");
        }
        ShellNotifyAssociationChanged();
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);

    private static void ShellNotifyAssociationChanged() => SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
}

internal sealed class MobileSignatureServer : IAsyncDisposable
{
    private readonly TaskCompletionSource<byte[]> _signature = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private WebApplication? _app;
    private string _token = Guid.NewGuid().ToString("N");

    public async Task<(string Url, Task<byte[]> SignatureTask)> StartAsync()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0); probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        var ip = GetLocalAddress();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        _app = builder.Build();
        _app.MapGet($"/{_token}", () => Results.Content(MobilePage, "text/html; charset=utf-8"));
        _app.MapPost($"/{_token}/signature", async (HttpRequest request) =>
        {
            if (request.ContentLength is <= 0 or > 5_000_000) return Results.StatusCode(413);
            if (!string.Equals(request.ContentType, "image/png", StringComparison.OrdinalIgnoreCase)) return Results.BadRequest("Formato inválido.");
            using var data = new MemoryStream(); await request.Body.CopyToAsync(data);
            if (data.Length == 0 || data.Length > 5_000_000) return Results.StatusCode(413);
            _signature.TrySetResult(data.ToArray());
            return Results.Text("Assinatura recebida. Pode fechar esta página.");
        });
        await _app.StartAsync();
        return ($"http://{ip}:{port}/{_token}", _signature.Task);
    }

    private static string GetLocalAddress()
    {
        var address = NetworkInterface.GetAllNetworkInterfaces()
            .Where(adapter => adapter.OperationalStatus == OperationalStatus.Up &&
                              adapter.NetworkInterfaceType is NetworkInterfaceType.Wireless80211 or NetworkInterfaceType.Ethernet)
            .SelectMany(adapter =>
            {
                var properties = adapter.GetIPProperties();
                var hasGateway = properties.GatewayAddresses.Any(gateway =>
                    gateway.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !gateway.Address.Equals(IPAddress.Any));
                return properties.UnicastAddresses
                    .Where(item => item.Address.AddressFamily == AddressFamily.InterNetwork && IsPrivateLanAddress(item.Address))
                    .Select(item => (item.Address, HasGateway: hasGateway, AdapterType: adapter.NetworkInterfaceType));
            })
            .OrderByDescending(item => item.HasGateway)
            .ThenBy(item => item.AdapterType == NetworkInterfaceType.Wireless80211 ? 0 : 1)
            .Select(item => item.Address)
            .FirstOrDefault();
        return address?.ToString() ?? IPAddress.Loopback.ToString();
    }

    internal static bool IsPrivateLanAddress(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork) return false;
        var bytes = address.GetAddressBytes();
        return bytes[0] == 10 ||
               (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is null) return;
        await _app.StopAsync();
        await _app.DisposeAsync();
        _app = null;
    }

    private const string MobilePage = """
<!doctype html><html lang="pt"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover"><meta name="color-scheme" content="light"><title>Assinar PDF</title><style>*{box-sizing:border-box}html,body{min-height:100%;background:#f5f6fa}body{font:17px system-ui,-apple-system,"Segoe UI",sans-serif;max-width:620px;margin:24px auto;padding:0 16px;color:#172033}h1{font-size:26px}canvas{display:block;width:100%;height:260px;background:#fff;border:2px dashed #8290a6;border-radius:16px;touch-action:none}button{padding:12px 18px;border:0;border-radius:10px;background:#245de8;color:#fff;font-size:16px;margin:12px 8px 0 0}#status{min-height:1.5em}</style></head><body><h1>Desenhe a assinatura</h1><p>A assinatura será enviada diretamente ao computador nesta rede.</p><canvas id="pad" width="1200" height="520"></canvas><button onclick="clearPad()">Limpar</button><button onclick="send()">Enviar assinatura</button><p id="status" role="status"></p><script>const c=document.querySelector('#pad'),x=c.getContext('2d');x.strokeStyle='#13244a';x.lineWidth=8;x.lineCap='round';let down=false;function pos(e){let r=c.getBoundingClientRect();return[(e.clientX-r.left)*c.width/r.width,(e.clientY-r.top)*c.height/r.height]}c.onpointerdown=e=>{down=true;c.setPointerCapture(e.pointerId);let p=pos(e);x.beginPath();x.moveTo(...p)};c.onpointermove=e=>{if(down){x.lineTo(...pos(e));x.stroke()}};c.onpointerup=()=>down=false;c.onpointercancel=()=>down=false;function clearPad(){x.clearRect(0,0,c.width,c.height)}async function send(){const status=document.querySelector('#status');status.textContent='A enviar…';try{let blob=await new Promise((resolve,reject)=>c.toBlob(b=>b?resolve(b):reject(new Error('Não foi possível gerar a imagem.')),'image/png'));let res=await fetch(location.pathname+'/signature',{method:'POST',headers:{'Content-Type':'image/png'},body:blob});if(!res.ok)throw new Error('O computador recusou a assinatura ('+res.status+').');status.textContent=await res.text()}catch(error){status.textContent='Falha ao enviar. Confirme se o PC continua ligado à mesma rede Wi-Fi. '+error.message}}</script></body></html>
""";
}

internal static class PdfSignatureService
{
    public static string AddVisualSignature(string input, string output, byte[] png, SignaturePlacement? placement = null)
    {
        using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);
        if (document.PageCount == 0) throw new InvalidDataException("O PDF não tem páginas.");
        using var imageStream = new MemoryStream(png);
        using var image = XImage.FromStream(imageStream);
        var page = document.Pages[0]; using var graphics = XGraphics.FromPdfPage(page);
        var rectangle = CalculateSignatureRectangle(page.Width.Point, page.Height.Point, image.PixelWidth, image.PixelHeight, placement);
        graphics.DrawImage(image, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        var candidate = output; var suffix = 2;
        while (File.Exists(candidate)) candidate = Path.Combine(Path.GetDirectoryName(output)!, $"{Path.GetFileNameWithoutExtension(output)} ({suffix++}){Path.GetExtension(output)}");
        document.Save(candidate);
        return candidate;
    }

    internal static SignatureRectangle CalculateSignatureRectangle(double pageWidth, double pageHeight, int imageWidth, int imageHeight, SignaturePlacement? placement)
    {
        var aspectRatio = (double)imageWidth / imageHeight;
        double width, x, y;
        if (placement is null)
        {
            width = Math.Min(pageWidth * 0.45, imageWidth);
            var height = width / aspectRatio;
            return new SignatureRectangle(pageWidth - width - 36, pageHeight - height - 36, width, height);
        }

        var widthRatio = Math.Clamp(placement.Width, 0.05, 0.8);
        width = pageWidth * widthRatio;
        var maxHeight = pageHeight * 0.8;
        if (width / aspectRatio > maxHeight) width = maxHeight * aspectRatio;
        var actualWidthRatio = width / pageWidth;
        x = Math.Clamp(placement.X, 0, 1 - actualWidthRatio) * pageWidth;
        y = Math.Clamp(placement.Y, 0, 1 - (width / aspectRatio) / pageHeight) * pageHeight;
        return new SignatureRectangle(x, y, width, width / aspectRatio);
    }
}

internal sealed record SignaturePlacement(double X, double Y, double Width);
internal readonly record struct SignatureRectangle(double X, double Y, double Width, double Height);
