using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.IO;
using SixLabors.ImageSharp.Processing;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;

namespace UI_conc
{
    public partial class MainWindow : Window
    {
        public string[] Emotions { get; } = { "neutral", "happiness", "surprise", "sadness", "anger", "disgust", "fear", "contempt" };
        public ObservableCollection<EmotionsImg> new_Data { get; set; } = new ObservableCollection<EmotionsImg>();
        public string selectedEmot { get; set; } = string.Empty;
        JArray db = JArray.Parse(File.ReadAllText("db.json"));
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            foreach (var el in db)
            {
                new_Data.Add(new EmotionsImg(new Bitmap(new MemoryStream(
                    JsonConvert.DeserializeObject<byte[]>((string?)el["blob"]))),
                    JsonConvert.DeserializeObject<List<(string, float)>>((string?)el["emotions"])));
            }
        }

        private void SortList(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var bw = new BackgroundWorker();
                bw.DoWork += (o, args) => DoSort();
                bw.RunWorkerCompleted += (o, args) => UpdateControl();
                bw.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while sorting data: {ex.Message}.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private (string,float) FindEl(IEnumerable<(string, float)> en)
        {
            foreach(var el in en)
            {
                if (el.Item1 == selectedEmot)
                    return el;
            }
            return en.First();
        }
        private void DoSort()
        {
            new_Data = new ObservableCollection<EmotionsImg>(new_Data.OrderByDescending(x => FindEl(x.Emotions).Item2));
        }

        private void UpdateControl()
        {
            listBox.ItemsSource = new_Data;
        }


        private async void DoAdd()
        {
            
            var dlg = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            dlg.Filter = "Image Files(*.BMP;*.JPG;*.PNG)|*.BMP;*.JPG;*.PNG";
            dlg.Multiselect = true;
            var result = dlg.ShowDialog();
            if (result == true)
            {
                await App.Current.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    execution.Visibility = Visibility.Visible;
                });


                var cancelTokenSource = new CancellationTokenSource();
                var token = cancelTokenSource.Token;

                var tasks = new Task<float[]>[dlg.FileNames.Length];
                var exec = new bool[dlg.FileNames.Length];
                var imgs = new Image<Rgb24>[dlg.FileNames.Length];
                for (int i = 0; i < dlg.FileNames.Length; i++)
                {
                    imgs[i] = Image<Rgb24>.Load<Rgb24>(dlg.FileNames[i]);
                    imgs[i].Mutate(ctx =>
                    {
                        ctx.Resize(new SixLabors.ImageSharp.Size(64, 64));
                    });
                    var blob = BitmapToArray(ToBitmap(imgs[i]));
                    var hash = 17;
                    foreach (byte element in blob)
                    {
                        hash = hash * 31 + element.GetHashCode();
                    }
                    var similar = db.SelectTokens($"$[?(@.hash == {hash})]");
                    exec[i] = true;
                    foreach (var el in similar)
                    {
                        var el_bl = JsonConvert.DeserializeObject<byte[]>((string?)el["blob"]);
                        if (el_bl.SequenceEqual(blob))
                        {
                            await App.Current.Dispatcher.BeginInvoke((Action)delegate ()
                            {
                                var img_from_db = new EmotionsImg(new Bitmap(new MemoryStream(el_bl)), 
                                    JsonConvert.DeserializeObject<List<(string, float)>>((string?)el["emotions"]));
                                new_Data.Add(img_from_db);
                            });
                            exec[i] = false;
                            break;
                        }
                    }
                    if (exec[i])
                    {
                        tasks[i] = EmotDetection.EmotionProbability(imgs[i], token);
                    }
                }

                for (int i = 0; i < dlg.FileNames.Length; i++)
                {
                    if (exec[i])
                    {
                        tasks[i].Wait();
                        await App.Current.Dispatcher.BeginInvoke((Action)delegate()
                        {
                            var res = ToJObj(ToBitmap(imgs[i]), EmotDetection.ZipWithKeys(tasks[i].Result));
                            db.Add(res);
                            new_Data.Add(new EmotionsImg(ToBitmap(imgs[i]), EmotDetection.ZipWithKeys(tasks[i].Result)));
                        });
                    }
                }
                File.WriteAllText("db.json", db.ToString());
                new_Data = new ObservableCollection<EmotionsImg>(new_Data.OrderByDescending(x => FindEl(x.Emotions).Item2));
                await App.Current.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    execution.Visibility = Visibility.Hidden;
                });
            }
        }

        public static Bitmap ToBitmap( Image<Rgb24> image)
        {
            using (var memoryStream = new MemoryStream())
            {
                image.SaveAsJpeg(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return new System.Drawing.Bitmap(memoryStream);
            }
        }

        public static byte[] ToArray(Image<Rgb24> image)
        {
            using (var ms = new MemoryStream())
            {
                image.SaveAsJpeg(ms);
                return ms.ToArray();
            }
        }

        public static byte[] BitmapToArray(System.Drawing.Bitmap img)
        {
            using (var stream = new MemoryStream())
            {
                var tmp = new Bitmap(img);
                tmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        public static JObject ToJObj(Bitmap image, IEnumerable<(string, float)> emotions)
        {
            var blob = BitmapToArray(image);
            var hash = 17;
            foreach (byte element in blob)
            {
                hash = hash * 31 + element.GetHashCode();
            }
            var obj = new JObject(
                new JProperty("hash", hash),
                new JProperty("blob", JsonConvert.SerializeObject(blob)),
                new JProperty("emotions", JsonConvert.SerializeObject(new List<(string, float)>(emotions)))
            );
            return obj;
        }


        private void Open(object sender, RoutedEventArgs args)
        {
            try
            {
                new_Data = new ObservableCollection<EmotionsImg>();
                var bw = new BackgroundWorker();
                bw.DoWork += (o, args) => DoAdd();
                bw.RunWorkerCompleted += (o, args) => UpdateControl();
                bw.RunWorkerAsync();
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error while load data: {e.Message}.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteFromDB(object sender, RoutedEventArgs args)
        {
            Button cmd = (Button)sender;
            if (cmd.DataContext is EmotionsImg)
            {

                var deleteme = (EmotionsImg)cmd.DataContext;


                var blob = BitmapToArray(deleteme.Image);
                var hash = 17;
                foreach (byte element in blob)
                {
                    hash = hash * 31 + element.GetHashCode();
                }
                var similar = db.SelectTokens($"$[?(@.hash == {hash})]");
                foreach (var el in similar)
                {
                    var el_bl = JsonConvert.DeserializeObject<byte[]>((string?)el["blob"]);
                    if (el_bl.SequenceEqual(blob))
                    {
                        el.Remove();
                        break;
                    }
                }
                File.WriteAllText("db.json", db.ToString());
                new_Data.Remove(deleteme);
            }
        }
    }
}
