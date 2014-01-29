using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Runtime.InteropServices.WindowsRuntime;

namespace WritableBitmapTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        WriteableBitmap wb = null;
        WriteableBitmap wbEraser = null;

        byte[] pixels;
        byte[] pixelsEraser;

        int eraserDiameter = 10;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            wb = new WriteableBitmap(1, 1);
            wbEraser = new WriteableBitmap(1, 1);

            image.Source = wb;
            LoadMask();
        }

        private async void LoadMask()
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/mask.png"));

            using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                try
                {
                    await wb.SetSourceAsync(fileStream);

                    using (Stream stream = wb.PixelBuffer.AsStream())
                    {
                        pixels = new byte[wb.PixelBuffer.Length];
                        wb.PixelBuffer.CopyTo(pixels);
                    }
                }
                catch (TaskCanceledException)
                {
                    // The async action to set the WriteableBitmap's source may be canceled if the source is changed again while the action is in progress
                }
            }

            file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/EraserBrush.png"));
            using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                try
                {
                    await wbEraser.SetSourceAsync(fileStream);

                    using (Stream stream = wbEraser.PixelBuffer.AsStream())
                    {
                        pixelsEraser = new byte[wbEraser.PixelBuffer.Length];
                        wbEraser.PixelBuffer.CopyTo(pixelsEraser);
                        eraserDiameter = wbEraser.PixelWidth;
                    }
                }
                catch (TaskCanceledException)
                {
                    // The async action to set the WriteableBitmap's source may be canceled if the source is changed again while the action is in progress
                }
            }

        }

        private async void image_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var pos = e.GetCurrentPoint(sender as UIElement);
            int row = (int)pos.Position.Y;
            int col = (int)pos.Position.X;

            int from_x = col - eraserDiameter / 2 + 36;
            int from_y = row - eraserDiameter / 2 + 36;

            for (int i = 0; i < eraserDiameter; i++)
            {
                for (int j = 0; j < eraserDiameter; j++)
                {
                    int indexEraser = j * wbEraser.PixelWidth * 4 + i * 4;
                    int index = (j + from_y) * wb.PixelWidth * 4 + (i + from_x) * 4;
                    if (index > 0 && index < pixels.Length)
                    {
                        byte alpha = pixelsEraser[indexEraser + 3];
                        double percent = (double)alpha / (double)255;

                        pixels[index + 0] = (byte)((double)pixels[index + 0] * percent);
                        pixels[index + 1] = (byte)((double)pixels[index + 1] * percent);
                        pixels[index + 2] = (byte)((double)pixels[index + 2] * percent);
                        pixels[index + 3] = (byte)((double)pixels[index + 3] * percent);
                    }
                }
            }

            using (Stream stream = wb.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(pixels, 0, pixels.Length);
            }
            wb.Invalidate();
        }
    }
}
