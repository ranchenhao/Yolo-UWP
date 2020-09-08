using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.AI.MachineLearning;
using YoloV2;

public class ObjectDetection
{
    public string info_text;

    private Model model = null;
    private string ModelFilename = "tinyyolov2-8.onnx";
    private Stopwatch TimeRecorder = new Stopwatch();

    public async Task LoadModelAsync()
    {
        ModifyText($"Loading {ModelFilename}... Patience");

        try
        {
            TimeRecorder = Stopwatch.StartNew();

            var modelFile = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri($"ms-appx:///Assets/{ModelFilename}"));
            model = await Model.CreateFromStreamAsync(modelFile);

            TimeRecorder.Stop();
            ModifyText($"Loaded {ModelFilename}: Elapsed time: {TimeRecorder.ElapsedMilliseconds} ms");
        }
        catch (Exception ex)
        {
            ModifyText($"error: {ex.Message}");
            model = null;
        }
    }

    public async Task<IList<YoloBoundingBox>> PredictImageAsync(SoftwareBitmap sfbmp)
    {
        IList<YoloBoundingBox> result = null;
        if (sfbmp != null)
        {
            try
            {
                TimeRecorder.Restart();

                Input inputData = new Input();
                await PreProcess(sfbmp, inputData);
                var output = await model.EvaluateAsync(inputData).ConfigureAwait(false);
                YoloOutputParser parser = new YoloOutputParser();
                var boundingBoxes = parser.ParseOutputs(output.grid.GetAsVectorView());
                result = parser.FilterBoundingBoxes(boundingBoxes, 5, .5F);

                TimeRecorder.Stop();

                string message = $"({DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second})" +
                    $" Evaluation took {TimeRecorder.ElapsedMilliseconds}ms\n";

                message = message.Replace("\\n", "\n");

                ModifyText(message);
            }
            catch (Exception ex)
            {
                var err_message = $"error: {ex.Message}";
                ModifyText(err_message);
            }
        }
        return result;
    }

    private void ModifyText(string text)
    {
        System.Diagnostics.Debug.WriteLine(text);
        info_text = text;
    }

    private async Task PreProcess(SoftwareBitmap sfbmp, Input input)
    {
        int iw = sfbmp.PixelWidth;
        int ih = sfbmp.PixelHeight;
        uint w, h;
        w = h = 416; // target image size
        SoftwareBitmap resizedBitMap;

        // Resize image
        using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
        {
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, stream);

            encoder.SetSoftwareBitmap(sfbmp);

            encoder.BitmapTransform.ScaledWidth = w;
            encoder.BitmapTransform.ScaledHeight = h;
            encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Cubic;

            await encoder.FlushAsync();

            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

            resizedBitMap = await decoder.GetSoftwareBitmapAsync(sfbmp.BitmapPixelFormat, sfbmp.BitmapAlphaMode);
        }
        VideoFrame inputImage = VideoFrame.CreateWithSoftwareBitmap(resizedBitMap);
        input.image = ImageFeatureValue.CreateFromVideoFrame(inputImage);
        
        /*
        input.input_1 = inputImage;
        input.image_shape = new float[1, 2];
        input.image_shape[0, 0] = ih;
        input.image_shape[0, 1] = iw;
        */
    }
}