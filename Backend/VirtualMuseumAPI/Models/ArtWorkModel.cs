using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace VirtualMuseumAPI.Models
{
    public class ArtWorkModel
    {
        
        /// <summary>
        /// The Artworks's unique ID
        /// </summary>
        public int ArtWorkID { get; set; }
        /// <summary>
        /// The Artist's unique id, this is an integer.
        /// </summary>
        [Required]
        public int ArtistID { get; set; }
        /// <summary>
        /// The artistmodel, so the name of the artist can be displayed
        /// </summary>
        public ArtistModel Artist { get; set; }
        /// <summary>
        /// The Artworks's name
        /// </summary>
        [Required]
        public string Name { get; set; }
        /// <summary>
        /// The Artworks's metadata
        /// </summary>
        [Required]
        public IEnumerable<KeyValuePair> Metadata { get; set; }

        /// <summary>
        /// How many times the artwork is collected
        /// </summary
        public int Collected { get; set; }      

    }
}