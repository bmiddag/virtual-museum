using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VirtualMuseumAPI.Models
{
    public class ArtWorkSearchModel
    {
        public string Name { get; set; }
        public int ArtistID { get; set; }
        public List<KeyValuePair> Filter { get; set; }
        public int ArtWorkFilterID { get; set; }
    }
}