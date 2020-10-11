using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dws.Classes;
using Newtonsoft.Json;

namespace dws.S
{
    public class ImageService
    {
        private static string imageQueue = "images.json";
        List<Image> images { get; set; } = new List<Image>();
        public ImageService()
        {
            images = GetQueue();
        }

        public List<Image> GetQueue()
        {
            List<Image> q = new List<Image>();
            if (File.Exists(imageQueue))
            {
                q = JsonConvert.DeserializeObject<List<Image>>(File.ReadAllText(imageQueue));
            }
            else { }
            return q;
        }

        public void AddImage(Image pic)
        {
            images.Add(pic);
            File.WriteAllText(imageQueue, JsonConvert.SerializeObject(images));
        }

        public void RemoveImage(Image pic)
        {
            images.Remove(pic);
            File.WriteAllText(imageQueue, JsonConvert.SerializeObject(images));
        }
        public void RemoveImage(int index)
        {
            images.RemoveAt(index);
            File.WriteAllText(imageQueue, JsonConvert.SerializeObject(images));
        }

        public Image GetImage(int v)
        {
            return GetQueue()[v];
        }
    }
}
