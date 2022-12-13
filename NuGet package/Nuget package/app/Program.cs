using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;


var cancelTokenSource = new CancellationTokenSource();
var token = cancelTokenSource.Token;
using Image<Rgb24> image1 = Image.Load<Rgb24>("face1.png");
using Image<Rgb24> image2 = Image.Load<Rgb24>("face2.png");

var task1 = EmotDetection.EmotionProbability(image1, token);
var task2 = EmotDetection.EmotionProbability(image2, token);

var res1 = EmotDetection.ZipWithKeys(await task1);
var res2 = EmotDetection.ZipWithKeys(await task2);

Console.WriteLine("Emotions on first image:");
foreach(var i in res1)
{
    Console.WriteLine($"{i.Item1} - {i.Item2}");
}

Console.WriteLine("Emotions on second image:");
foreach (var i in res2)
{
    Console.WriteLine($"{i.Item1} - {i.Item2}");
}