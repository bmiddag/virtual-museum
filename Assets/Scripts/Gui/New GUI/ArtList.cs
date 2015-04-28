﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using API;
using System;

public class ArtList : MonoBehaviour {
	
	public GUIControl artPopUp;
	
	public GUIControl popUpNormal;
	public Image popUpImage;
	public Text popUpTitle;
	public Text popUpArtist;
	public Text popUpDescription;
	
	// When you're the owner
	public ArtEditPanel popUpOwner;

	private bool started = false;
	GameObject listItem;
	GameObject separatorLine;
	int elementCount;
	public int userID = -1;
	public string userName = "";
	
	void Start () {
		listItem = (GameObject)Resources.Load("gui/ArtListItem");
		separatorLine = (GameObject)Resources.Load ("gui/ListItemSeparator");
		ClearList ();
		GetUserID ();
		InitList();
		started = true;
	}

	void OnEnable() {
		if(started) InitList ();
	}
	
	void ClearList() {
		for (int i = transform.childCount - 1; i >= 0; --i) {
			GameObject.Destroy(transform.GetChild(i).gameObject);
		}
		transform.DetachChildren();
		
		elementCount = 0;
	}
	
	public void InitList() {
		EventHandler handler = new EventHandler (OnArtLoaded);
		ClearList ();
		Catalog.RefreshArtWork (handler);		
	}

	public void OnArtLoaded(object sender, EventArgs e) {
		Art art = (Art)sender;
		ArtListItem item = ((GameObject) GameObject.Instantiate(listItem)).GetComponent<ArtListItem>();
		if(elementCount > 0) {
			GameObject separator = (GameObject)GameObject.Instantiate(separatorLine);
			separator.transform.SetParent(transform, false);
		}
		elementCount++;
		item.transform.SetParent (transform, false);
		item.list = this;
		item.artID = (art.ID == null ? -1 : art.ID);
		item.artArtist = (art.owner.name == null ? "" : art.owner.name);
		item.artDescription = (art.description == null ? "" : art.description);
		item.artTitle = (art.name == null ? "" : art.name);

		item.owner = (art.owner.ID == userID);

		item.artPopUp = artPopUp;
		item.popUpImage = popUpImage;
		item.popUpArtist = popUpArtist;
		item.popUpDescription = popUpDescription;
		item.popUpNormal = popUpNormal;
		item.popUpOwner = popUpOwner;
		item.popUpTitle = popUpTitle;
		item.artWork = art;

		item.UpdateLabels();
	}

	void GetUserID() {
		ArtistController control = ArtistController.Instance;
		control.GetConnectedArtists ((success) => {
			ArtListItem[] items = GetComponentsInChildren<ArtListItem>();
			foreach(API.Artist a in success) {
				userID = a.ID;
				userName = a.Name;
			}
			foreach(ArtListItem item in items) {
				item.owner = (userID == item.artID);
				item.UpdateLabels();
			}
		});
	}

	public void NewArt() {
		MainMenuActions actions = FindObjectOfType<MainMenuActions> ();
		if (actions != null) {
			actions.ResetArtID();
		}
		popUpOwner.artListItem = null;
		popUpOwner.gameObject.SetActive(true);
		popUpNormal.gameObject.SetActive(false);
		artPopUp.FlipCloseOpen ();
	}
}
