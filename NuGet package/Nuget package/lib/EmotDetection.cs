using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

static public class EmotDetection
{
    static private InferenceSession? session = null;

    static private readonly string[] keys = { "neutral", "happiness", "surprise", "sadness", "anger", "disgust", "fear", "contempt" };

    static private readonly object lockSession = new object();
    static public async Task<float[]> EmotionProbability(Image<Rgb24> img, CancellationToken token)
    {
        //Initialize new onnx session if it wasn't init yet
        if (session == null)
        {
            using var modelStream = typeof(EmotDetection).Assembly.GetManifestResourceStream("lib.emotion-ferplus-7.onnx");
            using var memoryStream = new MemoryStream();
            modelStream.CopyTo(memoryStream);
            session = new InferenceSession(memoryStream.ToArray());
        }

        //Get new task
        return await Task<float[]>.Factory.StartNew(() =>
        {
            token.ThrowIfCancellationRequested();
            img.Mutate(ctx =>
            {
                ctx.Resize(new Size(64, 64));
            });
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("Input3", GrayscaleImageToTensor(img)) };

            lock (lockSession)
            {
                token.ThrowIfCancellationRequested();
                var result = session.Run(inputs);
                return Softmax(result.First(v => v.Name == "Plus692_Output_0").AsEnumerable<float>().ToArray());
            }
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    static private float[] Softmax(float[] z)
    {
        var exps = z.Select(x => Math.Exp(x)).ToArray();
        var sum = exps.Sum();
        return exps.Select(x => (float)(x / sum)).ToArray();
    }

    static private DenseTensor<float> GrayscaleImageToTensor(Image<Rgb24> img)
    {
        var w = img.Width;
        var h = img.Height;
        var t = new DenseTensor<float>(new[] { 1, 1, h, w });

        img.ProcessPixelRows(pa =>
        {
            for (int y = 0; y < h; y++)
            {
                Span<Rgb24> pixelSpan = pa.GetRowSpan(y);
                for (int x = 0; x < w; x++)
                {
                    t[0, 0, y, x] = pixelSpan[x].R; // B and G are the same
                }
            }
        });

        return t;
    }


    static public IEnumerable<(string, float)> ZipWithKeys(float[] emoteProp)
    {
        return keys.Zip(emoteProp);
    }
};