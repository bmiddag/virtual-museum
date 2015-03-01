﻿using UnityEngine;
using System.Collections;
using Art;
/**
 * Stub for testing, just saves the id;
 * */

namespace ScanEngine
{
    public class IDScanner : Scanner
    {

        public IDScanner()
        {
        }

        public ScanIdentity MakeScannable(ScanIdentity scanId, Scannable s)
        {
            return new ScanID(s.GetUniqueString());
        }


        public Scannable Scan(ScanIdentity scan)
        {
            ArtFilter scannable = new ArtFilter();

            return (Scannable)scannable;
        }
    }
}
