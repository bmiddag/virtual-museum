using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VirtualMuseumAPI.Helpers
{
    public class CreditActionType
    {
        public enum Actions
        {
            ENTERMUSEUM, 
            BUILDEDMUSEUM, 
            ADDEDARTWORK, 
            SCANNEDARTWORK, 
            SHAREDLINK,
            EDITEDMUSEUM
        };
    }
}