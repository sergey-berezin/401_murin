using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI_conc
{
    public class EmotionsImg
    {
        public string FileName { get; set; }
        public IEnumerable<(string, float)> Emotions { get; set; }

        public EmotionsImg(string fileName, IEnumerable<(string,float)> emotions)
        {
            FileName = fileName;
            Emotions = emotions.OrderByDescending(x => x.Item2);
        }
    }
}
