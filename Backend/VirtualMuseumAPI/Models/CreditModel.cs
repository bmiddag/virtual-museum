using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using VirtualMuseumAPI.Helpers;

namespace VirtualMuseumAPI.Models
{
    public class CreditModel
    {

        /// <summary>
        /// The action type where the user gets credits for 
        /// </summary>
        [Required]
        public CreditActionType.Actions Actions { get; set; }

        /// <summary>
        /// The ID of the type that corresponds to the action
        /// </summary>
        public int ID { get; set; }
     
    }
}