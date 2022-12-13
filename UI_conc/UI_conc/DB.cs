using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UI_conc
{
    public class Img
    {
        [Key]
        public int ImgId { get; set; }
        public int Hash { get; set; }
        public byte[] Blob { get; set; }
        virtual public ICollection<Emotion> Emotions { get; set; }

        public Img()
        {
            
        }

        public Img(byte[] blob, IEnumerable<(string, float)> emotions)
        {
            Blob = blob;
            Hash = 17;
            Emotions = new List<Emotion>();
            foreach (byte element in blob)
            {
                Hash = Hash * 31 + element.GetHashCode();
            }
            foreach (var em in emotions)
            {
                Emotions.Add(new Emotion(em.Item1, em.Item2));
            }
        }
    }

    public class Emotion
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public float Prob { get; set; }

        public Emotion(string name, float prob)
        {
            Name = name;
            Prob = prob;
        }
    }

    public class ImagesContext : DbContext
    {
        public DbSet<Img> Images { get; set; }
        public DbSet<Emotion> Emotions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder o)
            => o.UseLazyLoadingProxies().UseSqlite("Data Source=library.db");
    }
}
