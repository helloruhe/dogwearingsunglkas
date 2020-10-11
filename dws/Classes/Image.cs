using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace dws.Classes
{
    public partial class Image
    {
        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("imagetype")]
        public ImageType ImageType { get; set; }
    }
    public enum ImageType
    {
        Image,
        Video,
        GIF
    }
}
