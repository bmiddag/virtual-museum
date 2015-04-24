﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using API;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

/// <summary>
/// Internal museum representation. This can load and save museum 
/// representations and has methods to modify the museum.
/// </summary>
public class Museum : MonoBehaviour, Savable<Museum, MuseumData>
{
    public Toast toast;
    public List<MuseumTile> tiles = new List<MuseumTile>();
    public List<MuseumObject> objects = new List<MuseumObject>();
    public List<MuseumArt> art = new List<MuseumArt>();
    public string ownerID;
    public string museumName;
    public int museumID = 0;
    public string description;
    public API.Level privacy;

    public Material frontMaterial;
    public Material backMaterial;

    public Texture2D debugTexture;

    private API.MuseumController cont;
    public HTTP.Request req;
    private Dictionary<int, Art> artDictionary = new Dictionary<int, Art>();
    private List<MuseumArt> artWaitingForDownload = new List<MuseumArt>();
    private HashSet<int> artIDsDownloading = new HashSet<int>();

    public void Start() {
        museumID = 0;
        SetTile(0, 0, 0, 0, 0, 0);
    }

    Art GetArt(int id, MuseumArt ma = null) {
        if (!artDictionary.ContainsKey(id)) {
            if (artIDsDownloading.Contains(id)) {
                return null;
            }
            artIDsDownloading.Add(id);
            Art art = new Art();
            ArtworkController.Instance.GetArtwork(
                "" + id,
                success: (artwork) => {
                    art.name = artwork.Name;
                    art.description = artwork.Name;
                    art.ID = artwork.ArtWorkID;
                    Debug.Log("Loaded");
                },
                error: (error) => {
                });
            ArtworkController.Instance.GetArtworkData(
                "" + id,
                success: (artwork) => {
                    art.image = new Texture2D(1, 1);
                    art.image.LoadImage(artwork);
                    Debug.Log("Loaded2");
                    artDictionary.Add(id, art);
                    artIDsDownloading.Remove(id);
                },
                error: (error) => {
                });
            return null;
        }
        return artDictionary[id];
    }

    /// <summary>
    /// Create a MuseumData for serialization.
    /// </summary>
    /// <returns>The MuseumData</returns>
    public MuseumData Save() {
        var tileData = new List<MuseumTileData>();
        foreach (var t in tiles) {
            tileData.Add(t.Save());
        }
        var artData = new List<MuseumArtData>();
        foreach (var a in art) {
            artData.Add(a.Save());
        }
        var objectData = new List<MuseumObjectData>();
        foreach (var o in objects) {
            objectData.Add(o.Save());
        }
        return new MuseumData(tileData, artData, objectData, ownerID, museumName, description, museumID, privacy);
    }

    /// <summary>
    /// Load a MuseumData inside this museum.
    /// </summary>
    /// <param name="data"></param>
    public void Load(MuseumData data) {
        Clear();
        foreach (var tileData in data.Tiles) {
            SetTile(tileData.WallStyle, tileData.FloorStyle, tileData.CeilingStyle, tileData.X, tileData.Y, tileData.Z);
        }
        foreach (var artData in data.Art) {
            AddArt(artData.Art.ID, new Vector3(artData.X, artData.Y, artData.Z), new Vector3(artData.RX, artData.RY, artData.RZ), artData.Scale, artData.FrameStyle);
        }
        foreach (var objectData in data.Objects) {
            AddObject(objectData.ObjectID, objectData.X, objectData.Y, objectData.Z, objectData.Angle);
        }
        ownerID = data.OwnerID;
        museumName = data.MuseumName;
        description = data.Description;
        museumID = data.MuseumId;
        privacy = data.Privacy;
    }

    public DateTime LastModified(string identifier)
    {
        return new DateTime();
    }

    /// <summary>
    /// Remove everything inside this museum.
    /// </summary>
    public void Clear() {
        foreach (var t in tiles) {
            t.Remove();
            Util.Destroy(t.gameObject);
        }
        tiles.Clear();
        foreach (var a in art) {
            a.Remove();
			Util.Destroy(a.gameObject);
        }
        art.Clear();
        foreach (var o in objects) {
            o.Remove();
			Util.Destroy(o.gameObject);
        }
        objects.Clear();
        Start();
    }

    /// <summary>
    /// Adds an artwork with an exact position and rotation
    /// </summary>
    /// <param name="artID"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    public void AddArt(int artID, Vector3 position, Vector3 rotation, float scale, int frameStyle = 0) {
        var normal = Quaternion.Euler(rotation) * Vector3.forward;
        int x = (int)Mathf.Floor(position.x + normal.x / 2 + 0.5f);
        int y = 0;
        int z = (int)Mathf.Floor(position.z + normal.z / 2 + 0.5f);
        RemoveArt(x, y, z);
        MuseumArt ma = new GameObject().AddComponent<MuseumArt>();
        
        Art a = GetArt(artID,ma);
        if (a == null) {
            a = new Art();
            a.ID = artID;
            artWaitingForDownload.Add(ma);
        }
        ma.position = position;
        ma.rotation = rotation;
		ma.frameStyle = frameStyle;
        ma.texture = debugTexture;
        ma.art = a;
        ma.tileX = x;
        ma.tileY = y;
        ma.tileZ = z;
        ma.scale = scale;
        art.Add(ma);
    }

    /// <summary></summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns>True if the coordinate contains art</returns>
    public bool ContainsArt(int x, int y, int z) {
        foreach(MuseumArt a in art){
            if (a.tileX == x && a.tileZ == z) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns the art at position x,y,z. Returns null when there is none.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns>Art at the coordinate, or null when there is none.</returns>
    public MuseumArt GetArt(int x, int y, int z) {
        foreach (MuseumArt a in art) {
            if (a.tileX == x && a.tileZ == z) {
                return a;
            }
        }
        return null;
    }

    /// <summary>
    /// Removes the art at the coordinate.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public void RemoveArt(int x, int y, int z) {
        MuseumArt toRemove = null;
        foreach (MuseumArt a in art) {
            if (a.tileX == x && a.tileZ == z) {
                toRemove = a;
            }
        }
        if (toRemove != null) {
            art.Remove(toRemove);
            toRemove.Remove();
			Util.Destroy(toRemove.gameObject);
        }
    }

    /// <summary>
    /// Add an object, only works if there is already a tile at the coordinate.
    /// </summary>
    /// <param name="objectID"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="angle"></param>
    public void AddObject(int objectID, int x, int y, int z, float angle) {
        if (ContainsTile(x, y, z)) {
            RemoveObject(x, y, z);
            var ob = new GameObject();
            var museumObject = ob.AddComponent<MuseumObject>();
            museumObject.objectID = objectID;
            museumObject.x = x;
            museumObject.y = y;
            museumObject.z = z;
            museumObject.angle = angle;
            objects.Add(museumObject);
        }
    }

    /// <summary>
    /// Removes the object at x,y,z if it exists.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public void RemoveObject(int x, int y, int z) {
        MuseumObject toRemove = null;
        foreach (MuseumObject o in objects) {
            if (o.x == x && o.y == y && o.z == z) {
                toRemove = o;
            }
        }
        if (toRemove != null) {
            objects.Remove(toRemove);
            toRemove.Remove();
			Util.Destroy(toRemove.gameObject);
        }
    }

    /// <summary></summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns>True if the coordinate contains an object.</returns>
    public bool ContainsObject(int x, int y, int z) {
        foreach (MuseumObject o in objects) {
            if (o.x == x && o.y == y && o.z == z) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns>The object at x,y,z if it exists, null otherwise.</returns>
    public MuseumObject GetObject(int x, int y, int z) {
        foreach (MuseumObject o in objects) {
            if (o.x == x && o.y == y && o.z == z) {
                return o;
            }
        }
        return null;
    }

    /// <summary>
    /// Sets a tile using a wall, floor and ceiling-style at a position.
    /// </summary>
    /// <param name="wallStyle"></param>
    /// <param name="floorStyle"></param>
    /// <param name="ceilingStyle"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public void SetTile(int wallStyle = 0, int floorStyle = 0, int ceilingStyle = 0, int x = 0, int y = 0, int z = 0) {
        RemoveTile(x, y, z, true);
        GameObject tileObject = new GameObject();
        tileObject.transform.parent = transform.parent;
        tileObject.transform.localPosition = new Vector3(x, y, z);
        var tile = tileObject.AddComponent<MuseumTile>();
        tiles.Add(tile);
        tile.x = x;
        tile.y = y;
        tile.z = z;
        tile.wallStyle = wallStyle;
        tile.floorStyle = floorStyle;
        tile.ceilingStyle = ceilingStyle;
        tile.frontMaterial = frontMaterial;
        tile.backMaterial = backMaterial;
        var leftTile = GetTile(x - 1, y, z);
        var rightTile = GetTile(x + 1, y, z);
        var frontTile = GetTile(x, y, z + 1);
        var backTile = GetTile(x, y, z - 1);
        if (leftTile == null) {
            tile.left = true;
        } else {
            leftTile.right = false;
        }
        if (rightTile == null) {
            tile.right = true;
        } else {
            rightTile.left = false;
        }
        if (frontTile == null) {
            tile.front = true;
        } else {
            frontTile.back = false;
        }
        if (backTile == null) {
            tile.back = true;
        } else {
            backTile.front = false;
        }
        if (leftTile != null){ 
            leftTile.UpdateEdges();
        }
        if (rightTile != null){ 
            rightTile.UpdateEdges();
        }
        if (backTile != null){ 
            backTile.UpdateEdges();
        }
        if (frontTile != null) {
            frontTile.UpdateEdges();
        }
    }

    /// <summary>
    /// Remove the tile at x,y,z and everything it might contain.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="forced">Overrides any exceptions that would prevent you from removing a tile.</param>
    public void RemoveTile(int x, int y, int z, bool forced = false) {
        var tile = GetTile(x, y, z);
        if (tile != null) {
            RemoveArt(x, y, z);
            RemoveObject(x, y, z);
            tiles.Remove(tile);
            tile.Remove();
			Util.Destroy(tile.gameObject);
            var leftTile = GetTile(x - 1, y, z);
            var rightTile = GetTile(x + 1, y, z);
            var frontTile = GetTile(x, y, z + 1);
            var backTile = GetTile(x, y, z - 1);
            if (leftTile != null) {
                leftTile.right = true;
            }
            if (rightTile != null){ 
                rightTile.left = true;
            }
            if (frontTile != null){ 
                frontTile.back = true;
            }
            if (backTile != null){ 
                backTile.front = true;
            }
            if (leftTile != null){ 
                leftTile.UpdateEdges();
            }
            if (rightTile != null){ 
                rightTile.UpdateEdges();
            }
            if (backTile != null){
                backTile.UpdateEdges();
            }
            if (frontTile != null){ 
                frontTile.UpdateEdges();
            }
            if (x == 0 && y == 0 && z == 0 && !forced) {
                SetTile(0, 0, 0, 0, 0, 0);
            }
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns>True if there is a tile at x,y,z.</returns>
    public bool ContainsTile(int x, int y, int z) {
        return GetTile(x, y, z) != null;
    }

    /// <summary>
    /// Returns the tile at position x,y,z.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns>The tile at position x,y,z if it exists, null otherwise.</returns>
    public MuseumTile GetTile(int x, int y, int z) {
        foreach (MuseumTile tile in tiles) {
            if (tile.x == x && tile.y == y && tile.z == z) {
                return tile;
            }
        }
        return null;
    }

	
	void Update () {
        List<MuseumArt> toUpdate = new List<MuseumArt>();
        foreach (MuseumArt museumArt in artWaitingForDownload) {
            if (artDictionary.ContainsKey(museumArt.art.ID)) {
                toUpdate.Add(museumArt);
            }
        }
        foreach (MuseumArt museumArt in toUpdate) {
            if (art.Contains(museumArt)) {
                museumArt.art = artDictionary[museumArt.art.ID];
                museumArt.Reload();
            }
            artWaitingForDownload.Remove(museumArt);
        }
	}


    public string getFolder()
    {
        return "museums/" + ownerID;
    }

    public string getFileName()
    {
        if (museumID != null) return "id_" + museumID + "_name_" + museumName.Replace(' ', '_');
        else return "name_" + museumName.Replace(' ', '_');
    }

    public string getExtension()
    {
        return "mus";
    }

    public void SaveRemote()
    {
        cont = API.MuseumController.Instance;
        byte[] data;
        BinaryFormatter bf = new BinaryFormatter();
        using (MemoryStream ms = new MemoryStream())
        {
            MuseumData md = Save();
            bf.Serialize(ms, md);
            data = ms.ToArray();
        }
        API.Museum museum = new API.Museum();
        museum.Description = this.description;
        museum.LastModified = DateTime.Now;
        museum.Privacy = this.privacy;
        museum.Name = this.museumName;
        museum.OwnerName = "";
        AsyncLoader loader = AsyncLoader.CreateAsyncLoader(
            () => {
                toast.Notify("Saving Museum...");
            },
            () => {
                toast.Notify("Museum saved!");
            });
        if (museumID == 0) {
            req = cont.CreateMuseum(museum, (mus) => {
                museumID = mus.MuseumID;
                req = cont.UploadMuseumData("" + mus.MuseumID, museumName, data);
                loader.forceDone = true;
            },
            (error) => { Debug.Log(error + " Something went wrong"); });
        } else {
            museum.MuseumID = museumID;
            req = cont.UpdateMuseum(museum, (mus) => {
                req = cont.UploadMuseumData("" + mus.MuseumID, museumName, data);
                loader.forceDone = true;
            });
        }
    }

    public void LoadRemote(string identifier)
    {
        museumID = Convert.ToInt32(identifier);
        cont = API.MuseumController.Instance;
        req = cont.GetMuseum(identifier,
            success: (museum) => {
                description = museum.Description;
                museumName = museum.Name;
                privacy = museum.Privacy;
                museumID = Convert.ToInt32(identifier);
            });
        req = cont.GetMuseumData(identifier,
            success: (museum) => {
                Stream stream = new MemoryStream(museum);
                BinaryFormatter deserializer = new BinaryFormatter();
                MuseumData data = (MuseumData)deserializer.Deserialize(stream);
                Load(data);
                museumID = Convert.ToInt32(identifier);
            });
    }

    public void DebugRegister() {
        var controller = API.UserController.Instance;
        controller.CreateUser("RianTest", "riangoossens@mailinator.com", "Password123/",
            (success) => {
                toast.Notify("Successfully registered!");
            });
    }

    public void DebugLogin() {
        var controller = API.UserController.Instance;
        controller.Login("RianTest", "Password123/",
            (success) => {
                SessionManager.Instance.LoginUser(success);
                toast.Notify("Successfully logged in!");
            });
    }

}
