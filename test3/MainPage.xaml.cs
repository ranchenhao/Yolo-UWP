using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace test3
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.objectDetection = new ObjectDetection();
        }

        public ObjectDetection objectDetection;

        public async void Image_Loaded(object sender, RoutedEventArgs e)
        {
            StorageFile storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/test.jpg"));
            SoftwareBitmap softwareBitmap;

            using (IRandomAccessStream stream = await storageFile.OpenAsync(FileAccessMode.Read))
            {
                // Create the decoder from the stream
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                // Get the SoftwareBitmap representation of the file
                softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            }

            // Draw original image
            Image img = sender as Image;
            img.Source = new BitmapImage(new Uri("ms-appx:///Assets/test.jpg"));

            // Predict image
            await objectDetection.LoadModelAsync();
            var boundingboxes = await objectDetection.PredictImageAsync(softwareBitmap);

            // Draw bounding box
            var originalImageWidth = softwareBitmap.PixelWidth;
            var originalImageHeight = softwareBitmap.PixelHeight;

            foreach (var box in boundingboxes)
            {
                var x = (uint)Math.Max(box.Dimensions.X, 0);
                var y = (uint)Math.Max(box.Dimensions.Y, 0);
                var width = (uint)Math.Min(originalImageWidth - x, box.Dimensions.Width);
                var height = (uint)Math.Min(originalImageHeight - y, box.Dimensions.Height);

                x = (uint)originalImageWidth * x / 416;
                y = (uint)originalImageHeight * y / 416;
                width = (uint)originalImageWidth * width / 416;
                height = (uint)originalImageHeight * height / 416;

                TextBlock text = new TextBlock();
                text.Text = $"{box.Label} ({(box.Confidence * 100).ToString("0")}%)";
                text.FontSize = 20;
                text.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                canvas.Children.Add(text);
                Canvas.SetLeft(text, x);
                Canvas.SetTop(text, y - 25);

                Rectangle rect = new Rectangle();
                rect.Width = width;
                rect.Height = height;
                rect.Fill = new SolidColorBrush(Windows.UI.Colors.Blue);
                rect.Fill.Opacity = 0;
                rect.Stroke = new SolidColorBrush(Windows.UI.Colors.Black);
                rect.StrokeThickness = 3;

                canvas.Children.Add(rect);
                Canvas.SetTop(rect, y);
                Canvas.SetLeft(rect, x);
            }
        }
    }
}
