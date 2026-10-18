using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ConversorFicheiros;

public partial class SignaturePlacementWindow : Window
{
    private readonly double _aspectRatio;
    private bool _isDragging;
    private Point _dragOffset;

    internal SignaturePlacement? Placement { get; private set; }

    public SignaturePlacementWindow(BitmapSource pagePreview, byte[] signaturePng, double pageWidthPoints, double pageHeightPoints)
    {
        InitializeComponent();
        PdfPreviewImage.Source = pagePreview;
        SignatureImage.Source = LoadBitmap(signaturePng);
        _aspectRatio = (double)SignatureImage.Source.Width / SignatureImage.Source.Height;

        var scale = Math.Min(720 / pageWidthPoints, 570 / pageHeightPoints);
        PageCanvas.Width = Math.Max(1, pageWidthPoints * scale);
        PageCanvas.Height = Math.Max(1, pageHeightPoints * scale);
        PlacementCanvas.Width = PageCanvas.Width;
        PlacementCanvas.Height = PageCanvas.Height;

        SizeSlider.Value = 32;
        SetSignatureWidth(PageCanvas.Width * SizeSlider.Value / 100);
        Canvas.SetLeft(SignatureBounds, PageCanvas.Width - SignatureBounds.Width - PageCanvas.Width * 0.04);
        Canvas.SetTop(SignatureBounds, PageCanvas.Height - SignatureBounds.Height - PageCanvas.Height * 0.05);
        UpdateSizeLabel();
    }

    private static BitmapImage LoadBitmap(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PageCanvas is null || SignatureBounds is null || _aspectRatio <= 0) return;
        SetSignatureWidth(PageCanvas.Width * SizeSlider.Value / 100);
        ClampSignaturePosition();
        UpdateSizeLabel();
    }

    private void SetSignatureWidth(double width)
    {
        var maxWidth = Math.Min(PageCanvas.Width * 0.8, PageCanvas.Height * 0.8 * _aspectRatio);
        width = Math.Clamp(width, PageCanvas.Width * 0.08, maxWidth);
        SignatureBounds.Width = width;
        SignatureBounds.Height = width / _aspectRatio;
    }

    private void SignatureBounds_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == ResizeThumb || IsDescendantOf(e.OriginalSource as DependencyObject, ResizeThumb)) return;
        _isDragging = true;
        _dragOffset = e.GetPosition(SignatureBounds);
        SignatureBounds.CaptureMouse();
        e.Handled = true;
    }

    private void SignatureBounds_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        var position = e.GetPosition(PlacementCanvas);
        Canvas.SetLeft(SignatureBounds, Math.Clamp(position.X - _dragOffset.X, 0, PageCanvas.Width - SignatureBounds.Width));
        Canvas.SetTop(SignatureBounds, Math.Clamp(position.Y - _dragOffset.Y, 0, PageCanvas.Height - SignatureBounds.Height));
    }

    private void SignatureBounds_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        SignatureBounds.ReleaseMouseCapture();
    }

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var requestedWidth = SignatureBounds.Width + e.HorizontalChange;
        if (Math.Abs(e.VerticalChange) > Math.Abs(e.HorizontalChange))
            requestedWidth = SignatureBounds.Width + e.VerticalChange * _aspectRatio;
        SetSignatureWidth(requestedWidth);
        SizeSlider.Value = Math.Clamp(SignatureBounds.Width / PageCanvas.Width * 100, SizeSlider.Minimum, SizeSlider.Maximum);
        ClampSignaturePosition();
        UpdateSizeLabel();
        e.Handled = true;
    }

    private void ClampSignaturePosition()
    {
        Canvas.SetLeft(SignatureBounds, Math.Clamp(Canvas.GetLeft(SignatureBounds), 0, PageCanvas.Width - SignatureBounds.Width));
        Canvas.SetTop(SignatureBounds, Math.Clamp(Canvas.GetTop(SignatureBounds), 0, PageCanvas.Height - SignatureBounds.Height));
    }

    private void UpdateSizeLabel() => SizeText.Text = $"{SignatureBounds.Width / PageCanvas.Width:P0}";

    private static bool IsDescendantOf(DependencyObject? child, DependencyObject ancestor)
    {
        while (child is not null)
        {
            if (ReferenceEquals(child, ancestor)) return true;
            child = System.Windows.Media.VisualTreeHelper.GetParent(child);
        }
        return false;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Placement = new SignaturePlacement(
            Canvas.GetLeft(SignatureBounds) / PageCanvas.Width,
            Canvas.GetTop(SignatureBounds) / PageCanvas.Height,
            SignatureBounds.Width / PageCanvas.Width);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
