﻿using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using API;

public class ArtCatalog : MonoBehaviour
{


	void Start(){
		Catalog.RefreshArtWork ();
	}

    //show new art panel in catalog
	public void AddNewArtToCatalog (GUIControl content)
	{
		//create and add a clone of the contents defined child
		GUIControl item = content.AddDynamicChild ();

		//update UI elements
		item.GetComponent<ArtworkGUIData> ().Refresh();
	}
	// do this in IEnumerator, because the upload is blocking for the moment, replace later with asyncloader in the upload of Art
	public void PostArt (GUIControl content)
	{
		for (int i=0; i<content.transform.childCount; i++) {
			content.GetChild (i).GetComponent<ArtworkGUIData> ().Save ();
		}
	}
	
	private void AddArtToCatalog (Art art, GUIControl content)
	{
		//add new item to catalog
		GUIControl item = content.AddDynamicChild ();
       
		//copy info in item
		item.GetComponent<ArtworkGUIData>().Init(art);
	}
	//this method can be optimized a lot by not creating new objects for each refresh but byjust editing them
	private Dictionary<int, Art>.ValueCollection allArt;
	/// <summary>
	/// Refresh the specified content.
	/// </summary>
	/// <param name="content">Content.</param>
	public void Refresh(GUIControl content)
	{
		//clear the catalog
		content.RemoveAllChildren ();
		//get all art from catalog
		 allArt = Catalog.getAllArt ().Values;
		Debug.Log (Catalog.getAllArt ().Count);
		//load art in gui, check for each art is it's still loading or not
		foreach (Art art in allArt) {
			if(art.loadingImage){
				Debug.Log ("Art is still loading, image will not be displayed.");
			}else{
				AddArtToCatalog(art,content);
			}
		}
	}
}