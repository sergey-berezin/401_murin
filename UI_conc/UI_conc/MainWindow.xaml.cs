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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Reflection.Metadata;
using System.Security.Policy;

namespace UI_conc
{
    public partial class MainWindow : Window
    {
        public string[] Emotions { get; } = { "neutral", "happiness", "surprise", "sadness", "anger", "disgust", "fear", "contempt" };
        public ObservableCollection<EmotionsImg> new_Data { get; set; } = new ObservableCollection<EmotionsImg>();
        public string selectedEmot { get; set; } = string.Empty;

        CancellationTokenSource cancellationTokenSource;
        CancellationToken token;

        public ImagesContext db;
        public MainWindow()
        {
            cancellationTokenSource = new CancellationTokenSource();
            token = cancellationTokenSource.Token;
            InitializeComponent();
            DataContext = this;
            db = new ImagesContext();
            foreach (var el in db.Images)
            {
                new_Data.Add(new EmotionsImg(new Bitmap(new MemoryStream(el.Blob)), el.Emotions));
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

        private (string, float) FindEl(IEnumerable<(string, float)> en)
        {
            foreach (var el in en)
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
            await App.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                menu.IsEnabled = false;
            });

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
                    int hash = 17;
                    foreach (byte element in blob)
                    {
                        hash = hash * 31 + element.GetHashCode();
                    }
                    var similar = db.Images.Where(el => el.Hash.Equals(hash))
                        .Where(el => el.Blob.SequenceEqual(blob)).FirstOrDefault();
                    exec[i] = true;
                    if (similar != null)
                    {
                        await App.Current.Dispatcher.BeginInvoke((Action)delegate ()
                        {
                            var img_from_db = new EmotionsImg(new Bitmap(new MemoryStream(similar.Blob)),
                                new List<Emotion>(similar.Emotions));
                            new_Data.Add(img_from_db);
                        });
                        exec[i] = false;
                    }
                    else
                    {
                        tasks[i] = EmotDetection.EmotionProbability(imgs[i], token);
                    }
                }

                for (int i = 0; i < dlg.FileNames.Length; i++)
                {
                    if (exec[i])
                    {
                        try
                        {
                            tasks[i].Wait();
                            await App.Current.Dispatcher.BeginInvoke((Action)delegate ()
                            {
                                var res = new Img(BitmapToArray(ToBitmap(imgs[i])), EmotDetection.ZipWithKeys(tasks[i].Result));
                                db.Images.Add(res);
                                new_Data.Add(new EmotionsImg(ToBitmap(imgs[i]), EmotDetection.ZipWithKeys(tasks[i].Result)));
                            });
                        }
                        catch (AggregateException)
                        {

                        }
                    }
                }
                new_Data = new ObservableCollection<EmotionsImg>(new_Data.OrderByDescending(x => FindEl(x.Emotions).Item2));
                await App.Current.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    execution.Visibility = Visibility.Hidden;
                });
            }
            db.SaveChanges();
            await App.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                menu.IsEnabled = true;
            });
        }

        public static Bitmap ToBitmap(Image<Rgb24> image)
        {
            using (var memoryStream = new MemoryStream())
            {
                image.SaveAsJpeg(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return new Bitmap(memoryStream);
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

        public static byte[] BitmapToArray(Bitmap img)
        {
            using (var stream = new MemoryStream())
            {
                var tmp = new Bitmap(img);
                tmp.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
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
                new_Data.Remove(deleteme);

                var blob = BitmapToArray(deleteme.Image);
                var hash = 17;
                foreach (byte element in blob)
                {
                    hash = hash * 31 + element.GetHashCode();
                }
                var similar = db.Images.Where(el => el.Hash == hash)
                    .Where(el => el.Blob.SequenceEqual(blob)).FirstOrDefault();
                if (similar != null)
                {
                    db.Remove(similar);
                    db.SaveChanges();
                }
                listBox.ItemsSource = new_Data;
            }
        }

        private void CancelProcess(object sender, RoutedEventArgs args)
        {
            App.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                cancellationTokenSource.Cancel();
            });
        }
    }
}