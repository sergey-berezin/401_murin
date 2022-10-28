using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace UI_conc
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string[] Emotions { get; } = { "neutral", "happiness", "surprise", "sadness", "anger", "disgust", "fear", "contempt" };
        public ObservableCollection<EmotionsImg> new_Data { get; set; } = new ObservableCollection<EmotionsImg>();
        public string selectedEmot { get; set; } = string.Empty;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
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
                var cancelTokenSource = new CancellationTokenSource();
                var token = cancelTokenSource.Token;
                var tasks = new Task<float[]>[dlg.FileNames.Length];
                for (int i = 0; i < dlg.FileNames.Length; i++)
                {
                    tasks[i] = EmotDetection.EmotionProbability(Image<Rgb24>.Load<Rgb24>(dlg.FileNames[i]), token);
                }
                await Task.WhenAll(tasks);
                for (int i = 0; i < dlg.FileNames.Length; i++)
                {
                    await App.Current.Dispatcher.BeginInvoke((Action)delegate ()
                    {
                        new_Data.Add(new EmotionsImg(dlg.FileNames[i], EmotDetection.ZipWithKeys(tasks[i].Result)));
                    });
                }
                new_Data = new ObservableCollection<EmotionsImg>(new_Data.OrderByDescending(x => FindEl(x.Emotions).Item2));
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
    }
}
