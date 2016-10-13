using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace VirtualMuseumAPI.Models
{
    public class ArtWorkFilterModel
    {

        public List<KeyValuePair> Pairs { get; set; }
        [Required]
        public int ArtistID { get; set; }

        public int ArtWorkID { get; set; }

    }
}