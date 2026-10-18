using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ConversorFicheiros;
using PdfSharp.Pdf.IO;

namespace ConversorFicheiros.Tests;

[TestClass]
public class ConversionAndSignatureTests
{
    [TestMethod]
    public void Converter_RejectsUnsupportedTargetFormat()
    {
        var converter = new ImageConversionService();
        Assert.ThrowsException<NotSupportedException>(() => converter.Convert("missing.png", "webp"));
    }

    [TestMethod]
    public void Converter_WritesJpegAndPreservesEarlierOutput()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "test-artifacts", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            RunSta(() =>
            {
                var pixels = new byte[] { 0, 0, 255, 255, 0, 255, 0, 255, 255, 0, 0, 255, 255, 255, 255, 255 };
                var bitmap = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null, pixels, 8);
                var input = Path.Combine(directory, "sample.png");
                var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
                using (var stream = File.Create(input)) png.Save(stream);

                var converter = new ImageConversionService();
                var first = converter.Convert(input, "jpg");
                var second = converter.Convert(input, "jpg");
                var pdf = converter.Convert(input, "pdf");
                Assert.IsTrue(File.Exists(first));
                Assert.IsTrue(File.Exists(second));
                Assert.IsTrue(File.Exists(pdf));
                Assert.AreNotEqual(first, second);
                Assert.AreEqual(2, BitmapDecoder.Create(new Uri(first), BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0].PixelWidth);
                using var document = PdfReader.Open(pdf, PdfDocumentOpenMode.Import);
                Assert.AreEqual(1, document.PageCount);
            });
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [TestMethod]
    public async Task MobileServer_ReceivesPngAndCompletesSignatureTask()
    {
        await using var server = new MobileSignatureServer();
        var session = await server.StartAsync();
        var uri = new UriBuilder(session.Url) { Host = IPAddress.Loopback.ToString() }.Uri;
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        using var pageResponse = await client.GetAsync(uri);
        pageResponse.EnsureSuccessStatusCode();
        var page = await pageResponse.Content.ReadAsStringAsync();
        StringAssert.Contains(page, "Desenhe a assinatura");
        StringAssert.Contains(page, "<canvas id=\"pad\"");

        var png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3 };
        using var content = new ByteArrayContent(png);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

        using var response = await client.PostAsync(new Uri(uri.AbsoluteUri.TrimEnd('/') + "/signature"), content);
        response.EnsureSuccessStatusCode();
        CollectionAssert.AreEqual(png, await session.SignatureTask.WaitAsync(TimeSpan.FromSeconds(3)));
    }

    [TestMethod]
    public void Catalog_OffersWordToPdfAndPdfToDocx()
    {
        CollectionAssert.Contains(ConversionCatalog.TargetsFor("report.docx").ToArray(), "pdf");
        CollectionAssert.Contains(ConversionCatalog.TargetsFor("report.pdf").ToArray(), "docx");
    }

    [TestMethod]
    public void MobileAddressSelection_ExcludesRadminVpnAddress()
    {
        Assert.IsFalse(MobileSignatureServer.IsPrivateLanAddress(IPAddress.Parse("26.157.241.137")));
        Assert.IsTrue(MobileSignatureServer.IsPrivateLanAddress(IPAddress.Parse("192.168.1.75")));
    }

    [TestMethod]
    public void PdfSignatureService_AddsSignatureAndCreatesOutputPdf()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "test-artifacts", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            RunSta(() =>
            {
                var input = Path.Combine(directory, "original.pdf");
                var output = Path.Combine(directory, "original_assinado.pdf");
                var pdf = new PdfSharp.Pdf.PdfDocument();
                pdf.AddPage();
                pdf.Save(input);

                var bitmap = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null,
                    new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 8);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var image = new MemoryStream();
                encoder.Save(image);

                var result = PdfSignatureService.AddVisualSignature(input, output, image.ToArray());
                Assert.IsTrue(File.Exists(result));
                using var signed = PdfReader.Open(result, PdfDocumentOpenMode.Import);
                Assert.AreEqual(1, signed.PageCount);
            });
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { action(); } catch (Exception ex) { failure = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(); thread.Join();
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
