﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Scanning;
using System.IO;

public class QRGenerator : MonoBehaviour {

    public ArtFilter RequestedArtFilter { get; set; }
    public QRView view;

    public QRGenerator(ArtFilter filter)
    {
        RequestedArtFilter = filter;   
    }

	// Use this for initialization
    void Start()
    {
        Debug.Log("Start is called");
        RequestedArtFilter = new ArtFilter();
        RequestedArtFilter.ArtistName = "testArtist";
        RequestedArtFilter.Tags.Add("testTag1");
        RequestedArtFilter.Tags.Add("testTag2");
        RequestedArtFilter.Genres.Add("testGenre1");
        RequestedArtFilter.Genres.Add("testGenre2");
        Debug.Log("ArtFilter made");
        QRScanner scanner = new QRScanner();
        QRCode qrCode = (QRCode)scanner.MakeScannable(null, RequestedArtFilter);
        Color32[] QRCodeImage = qrCode.Image;
        Debug.Log("QRCode image required");

        Texture2D qrTexture = new Texture2D(256, 256);
        Debug.Log("number of pixels in array:" + QRCodeImage.Length);
        qrTexture.SetPixels32(QRCodeImage);
        Color32[] pixels = qrTexture.GetPixels32();

        view.image = qrTexture;

        // Print the QRCode
        foreach (Color32 c in pixels)
        {
            if (c.r != 255 || c.g != 255 || c.b != 255)
                Debug.Log("pixels: non white pixel: " + c);
        }

        // Test if the QRView works
        /*
        Texture2D a = new Texture2D(256, 256);
        a.LoadImage(File.ReadAllBytes("B:\\Documents\\GitHub\\DesignProject\\Assets\\UI Sprites\\TestQR.jpg"));
        view.image = a;*/
    }

	// Update is called once per frame
	void Update () {
	    //this method can be left empty
	}
}
