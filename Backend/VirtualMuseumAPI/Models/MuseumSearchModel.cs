using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VirtualMuseumAPI.Models
{
    public class MuseumSearchModel
    {
        /// <summary>
        /// Substring of the description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Substring of the ownername that needs to be found
        /// </summary>
        public string OwnerName { get; set; }

        /// <summary>
        /// Substring of the museum name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The rating of the museums
        /// </summary>
        public int Rating { get; set; }
    }
}