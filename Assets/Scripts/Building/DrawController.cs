﻿using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// Controller that handles all input and uses it to modify both the scene and the
/// museum when inside the BuildMuseum scene.
/// </summary>
public class DrawController : MonoBehaviour {

    public enum Tools : int {
        Drawing,
        Moving,
        Rotating,
        Erasing,
        Scaling,
        PlacingObject,
        PlacingArt,
        Selecting
    }

    public GameObject toDraw;
    public Museum currentMuseum;
    public float cameraSpeed = 10;
    public Tools tool = Tools.Drawing;
	public BuildMuseumActions actions;

    public int currentArt = 1;
    public int currentObject = 0;
    public int currentFloor = 0;
    public int currentWall = 0;
    public int currentCeiling = 0;
	public int currentFrame = 0;

    private bool[] dragging = {false, false, false, false, false};
    private Vector3 centerPointWorld = Vector3.zero;
    private Vector3 anchorPointScreen = Vector3.zero;
    private Vector3 anchorPointWorld = Vector3.zero;
    private Vector3 anchorNormalWorld = Vector3.zero;
    private Vector3 lastDragPointScreen = Vector3.zero;
    private Vector3 cameraAnchor = Vector3.zero;

    private LayerMask groundLayerMask;
    private LayerMask wallLayerMask;

    public enum SelectionMode {
        Dragging,
        Rotating
    }

    private MuseumObject selectedObject;
    private SelectionMode currentSelectionMode = SelectionMode.Dragging;

	void Start () {
        groundLayerMask = (1 << LayerMask.NameToLayer("Ground"));
        wallLayerMask = (1 << LayerMask.NameToLayer("Walls"));
		//load art info & thumbnail
		Catalog.Refresh();
	}

    /// <summary>
    /// Change the current tool by using the toolID. One can use the Tools enum and convert it to an int as well.
    /// </summary>
    /// <param name="tool">The int representation of a Tools enum value.</param>
    public void SetTool(int tool) {
        this.tool = (Tools)tool;
		if (this.tool != Tools.Selecting) {
			currentMuseum.SetSelected(null);
		}
    }

    /// <summary>
    /// Change the current object by using the object's id.
    /// </summary>
    /// <param name="objectID"></param>
    public void SetCurrentObject(int objectID) {
        this.currentObject = objectID;
    }
	public void SetCurrentFrame(int frameID){
		this.currentFrame = frameID;
	}
	public void SetCurrentCeiling(int ceilingID){
		this.currentCeiling = ceilingID;
	}
	public void SetCurrentFloor(int floorID){
		this.currentFloor = floorID;
	}
	public void SetCurrentWall(int wallID){
		this.currentWall = wallID;
	}
	public void SetCurrentArt(int artID){
		this.currentArt = artID;
	}
    bool IsPointerBusy() {
        foreach (Touch touch in Input.touches) {
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) {
                return true;
            }
            if (touch.phase == TouchPhase.Ended) {
                return true;
            }
        }
        return EventSystem.current.IsPointerOverGameObject(0) || EventSystem.current.IsPointerOverGameObject();
    }

    RaycastHit raycast(Vector3 origin, Vector3 direction, float maxDistance, LayerMask layerMask) {
        RaycastHit info = new RaycastHit();
        if (Physics.Raycast(origin, direction, out info, maxDistance, layerMask)) {
            return info;
        }
        return new RaycastHit();
    }

    void Update() {
#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_EDITOR
        ToolUpdate(0, tool);
        MouseUpdate();
#endif
#if UNITY_IOS || UNITY_ANDROID
        if (Input.touchCount == 1) {
            ToolUpdate(0, tool);
        } else {
            GestureUpdate();
        }
#endif
    }
	
    void ToolUpdate(int mouseButton, Tools tool) {
        var mouse2D = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1);
        var mouse3D = Camera.main.ScreenToWorldPoint(mouse2D);
        Debug.DrawRay(mouse3D, Camera.main.transform.forward * 100, Color.white, 0.1f);
        var mask = groundLayerMask;
        if (tool == Tools.PlacingArt) {
            mask = wallLayerMask;
        } else if (tool == Tools.Erasing) {
            mask = wallLayerMask | groundLayerMask;
        }
        var click = false;
        if (Input.GetMouseButtonDown(mouseButton) && !IsPointerBusy()) {
            cameraAnchor = Camera.main.transform.position;
            dragging[mouseButton] = true;
            anchorPointScreen = mouse2D;
            var anchorWorld = raycast(mouse3D, Camera.main.transform.forward, Mathf.Infinity, mask);
            anchorPointWorld = anchorWorld.point;
            anchorNormalWorld = anchorWorld.normal;
            lastDragPointScreen = anchorPointScreen;
            centerPointWorld = raycast(cameraAnchor, Camera.main.transform.forward, Mathf.Infinity, groundLayerMask).point;
            click = true;
        }
        if (Input.GetMouseButtonUp(mouseButton)) {
			// This code destroys selected object if released above trash
			if(tool == Tools.Selecting && actions.toolButtons[(int)Tools.Erasing].IsHighlighted()) {
				currentMuseum.RemoveObject(selectedObject);
				selectedObject = null;
			}

            dragging[mouseButton] = false;
        }
        if (dragging [mouseButton]) {
			var dragPointScreen = Vector3.zero;
			var dragPointWorld = Vector3.zero;
			dragPointScreen = mouse2D;
			dragPointWorld = raycast (mouse3D, Camera.main.transform.forward, Mathf.Infinity, groundLayerMask).point;
			var dragOffsetScreen = anchorPointScreen - dragPointScreen;
			var dragOffsetWorld = anchorPointWorld - dragPointWorld;
			var frameOffsetScreen = dragPointScreen - lastDragPointScreen;
			switch (tool) {
			case Tools.Drawing:
				Draw (dragPointWorld);
				break;
			case Tools.Moving:
				Move (dragOffsetWorld);
				break;
			case Tools.Rotating:
				Rotate (centerPointWorld, new Vector3 (-frameOffsetScreen.y / Screen.height * 180, frameOffsetScreen.x / Screen.width * 180, 0));
				break;
			case Tools.Erasing:
				Erase (dragPointWorld, anchorPointWorld, anchorNormalWorld, click);
				break;
			case Tools.Scaling:
				Scale (Mathf.Pow (2, -frameOffsetScreen.y / Screen.height));
				break;
			case Tools.PlacingObject:
				PlaceObject (dragPointWorld, anchorPointWorld);
				break;
			case Tools.PlacingArt:
				PlaceArt (dragPointWorld, anchorPointWorld, anchorNormalWorld, dragPointScreen, anchorPointScreen);
				break;
			case Tools.Selecting:
				Select (dragPointWorld, anchorPointWorld, click);
				break;
			}
			lastDragPointScreen = dragPointScreen;
		}
    }

    void MouseUpdate() {
		if (actions.canScroll) {
			var scroll = Input.GetAxis ("Mouse ScrollWheel");
			if (scroll < 0) {
				Scale (1.1f);
			} else if (scroll > 0) {
				Scale (0.9f);
			}
		}
        ToolUpdate(1, Tools.Rotating);
        ToolUpdate(2, Tools.Moving);
    }

    void GestureUpdate() {
        if (Input.touchCount == 2 && actions.canScroll) {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;
            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
            Scale(Mathf.Pow(2, deltaMagnitudeDiff*2 / Screen.height));
        }
    }

    void Draw(Vector3 dragPointWorld) {
        currentMuseum.SetTile(currentWall, currentFloor, currentCeiling, (int)Mathf.Floor(dragPointWorld.x + 0.5f), 0, (int)Mathf.Floor(dragPointWorld.z + 0.5f));
    }

    void Erase(Vector3 dragPointWorld, Vector3 anchorPointWorld, Vector3 anchorNormalWorld, bool click) {
        int x = (int)Mathf.Floor(dragPointWorld.x + 0.5f);
        int y = 0;
        int z = (int)Mathf.Floor(dragPointWorld.z + 0.5f);
        if (click || Vector3.Distance(dragPointWorld, anchorPointWorld) > 0.1f) {
            var rotation = Quaternion.LookRotation(anchorNormalWorld).eulerAngles;
            if (currentMuseum.ContainsArt(anchorPointWorld,rotation)) {
                currentMuseum.RemoveArt(anchorPointWorld,rotation);
            } else if (currentMuseum.ContainsObject(x, y, z)) {
                currentMuseum.RemoveObject(x, y, z);
            } else {
                currentMuseum.RemoveTile(x, y, z);
            }
        }
    }

    void Move(Vector3 movement) {
        Camera.main.transform.Translate(movement, Space.World);
    }

    void Rotate(Vector3 center, Vector3 angles) {
        Camera.main.transform.RotateAround(center, Camera.main.transform.right, angles.x);
        Camera.main.transform.RotateAround(center, new Vector3(0, 1, 0), angles.y);
        Camera.main.transform.RotateAround(center, new Vector3(0, 0, 1), angles.z);
    }

    void Scale(float factor) {
        Camera.main.orthographicSize *= factor;
    }

    void PlaceObject(Vector3 dragPointWorld, Vector3 anchorPointWorld) {
        int x =  (int)Mathf.Floor(anchorPointWorld.x + 0.5f);
        int y = 0;
        int z = (int)Mathf.Floor(anchorPointWorld.z + 0.5f);
        currentMuseum.AddObject(currentObject,x, y, z, 0);
        tool = Tools.Selecting;
        currentSelectionMode = SelectionMode.Dragging;
        selectedObject = currentMuseum.GetObject(x, y, z);
    }

    void PlaceArt(Vector3 dragPointWorld, Vector3 anchorPointWorld, Vector3 anchorNormalWorld, Vector3 dragPointScreen, Vector3 anchorPointScreen) {
        if (Vector3.Magnitude(anchorNormalWorld) < 0.5) {
            return;
        }
        var diff = Vector3.Distance(anchorPointScreen, dragPointScreen);
        var scale = 0.5f + 4*diff / Screen.width;
        currentMuseum.AddArt(currentArt, anchorPointWorld, Quaternion.LookRotation(anchorNormalWorld).eulerAngles,scale,currentFrame);
    }

    void Select(Vector3 dragPointWorld, Vector3 anchorPointWorld, bool click) {
        var x = (int)Mathf.Floor(dragPointWorld.x + 0.5f);
        var y = 0;
        var z = (int)Mathf.Floor(dragPointWorld.z + 0.5f);
        if (click) {
            if (selectedObject != null) {
                var distance = Vector3.Distance(selectedObject.GetPosition(), dragPointWorld);
                if (distance > 0.5f && distance < 1.5f) {
                    currentSelectionMode = SelectionMode.Rotating;
                } else {
                    currentSelectionMode = SelectionMode.Dragging;
                }
            } else {
                currentSelectionMode = SelectionMode.Dragging;
            }
        }
        if (currentSelectionMode == SelectionMode.Dragging) {
            if (Vector3.Distance(dragPointWorld, anchorPointWorld) < 0.1f) {
                selectedObject = currentMuseum.GetObject(x, y, z);
            }
            if (selectedObject != null) {
                currentMuseum.MoveObject(selectedObject, x, y, z);
            }
            currentMuseum.SetSelected(selectedObject);
        } else if(currentSelectionMode == SelectionMode.Rotating) {
            var diff = (dragPointWorld - selectedObject.GetPosition()).normalized;
            var angle = -(Mathf.Atan2(diff.z, diff.x) - Mathf.PI / 2) / Mathf.PI * 180;
            selectedObject.angle = angle;
            selectedObject.SetRotation(angle);
        }
    }
}
