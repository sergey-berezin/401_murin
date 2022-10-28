using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI_conc
{
    internal class Emotion
    {
        public string Emot { get; }
        public float Prob { get; }
        public Emotion(string emot, float prob)
        {
            Emot = emot;
            Prob = prob;
        }
        public override string ToString()
        {
            return Emot + $": {Prob}";
        }
    }
}
