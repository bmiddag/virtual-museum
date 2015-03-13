﻿using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;


public class ArtRest : MonoBehaviour
{
	private List<ArtGUIInterface> allArt;
	private WWW www;
	//thread safe 
	IEnumerator  getAllArt (GUIControl content)
	{
		string imageArtworkUrl;
		API.UserController uc = API.UserController.Instance;
		API.ArtworkController ac = API.ArtworkController.Instance;

		allArt = new List<ArtGUIInterface> ();
		ArtGUIInterface newArtCatalogItem;
		content.removeAllChildren ();

		ac.getAllArtworks (success: (response) => {
			foreach(Hashtable child in response) {
				ac.getArtwork(child["ArtWorkID"].ToString(), success:(texture) => {
					newArtCatalogItem = new ArtGUIInterface (child ["ArtWorkID"].ToString(), child ["ArtistID"].ToString(), child ["Name"].ToString(), texture);
					allArt.Add (newArtCatalogItem);
					catalogItemFromGUIInterface (newArtCatalogItem,content);
				}, error:(error) => {Debug.Log("An error occured while loading artwork with ID: " + child["ArtWorkID"]);});

			}
		},
							error: (error) => {
			Debug.Log("An error occured while loading all artworks");
		}); 

		yield return null;
	}
	//post the edited art 
	IEnumerator  postArt (GUIControl content)
	{
		string artworkUrl = "http://api.awesomepeople.tv/api/artwork";
		www = new WWW (artworkUrl);
		yield return www;
	}
	public void postArt(ArtGUIInterface art){

	}
	private GUIControl catalogItemFromGUIInterface (ArtGUIInterface art,GUIControl content)
	{
		GUIControl item = GUIControl.init (GUIControl.types.CatalogItem);
		content.add (item);

		//change image
		//get the image from the catalog item
		Image image = item.transform.Find ("Preview").gameObject.GetComponent<Image> ();
		image.enabled = true;
		image.sprite = Sprite.Create (art.Thumbnail,new Rect(0, 0, art.Thumbnail.width, art.Thumbnail.height), Vector2.zero);
		item.normalise ();

		//change text
		Transform inputFields = item.transform.Find ("InputBox/InputFields").transform;
		//name
		InputField field = inputFields.GetChild(0).GetComponent<InputField> ();
		field.text = art.Name;

		//ID
		field = inputFields.GetChild(1).GetComponent<InputField> ();
		field.text = art.Id;
		//artist
		field = inputFields.GetChild(2).GetComponent<InputField> ();
		field.text = art.ArtistID;
		return item;
	}

	public void fillCatalogWithAllArt (GUIControl content)
	{

		StartCoroutine (getAllArt (content));

	}


	private const string BASE_URL = "http://api.awesomepeople.tv/";
	private const string TOKEN = "Token";
	private const string ARTWORK = "api/artwork";
}