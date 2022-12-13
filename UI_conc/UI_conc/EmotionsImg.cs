using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;

namespace UI_conc
{
    public class EmotionsImg
    {
        public Bitmap Image { get; set; }
        public ImageSource SourceImage
        {
            get
            {
                var handle = Image.GetHbitmap();
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
        }
        public IEnumerable<(string, float)> Emotions { get; set; }

        public EmotionsImg(Bitmap image, IEnumerable<(string, float)> emotions)
        {
            Image = image;
            Emotions = emotions.OrderByDescending(x => x.Item2);
        }
        public EmotionsImg(Bitmap image, IEnumerable<Emotion> emotions)
        {
            Image = image;
            Emotions = new List<(string, float)>();
            foreach (var em in emotions)
            {
                Emotions = Emotions.Append((em.Name, em.Prob));
            }
            Emotions = Emotions.OrderByDescending(x => x.Item2);
        }
    }
}
