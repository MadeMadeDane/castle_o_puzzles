using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemActionHandler : MonoBehaviour {
    public PlayerController pc;
    public InputManager im;
    public Camera cam;
    public Inventory actionSlots;
    // Use this for initialization
	void Start ()
    {
        actionSlots = gameObject.AddComponent<Inventory>();
        actionSlots.numSlots = 5;
	}
	
	// Update is called once per frame
	void Update ()
    {

        // Bit shift the index of the layer (8) to get a bit mask
        int layerMask = 0 << 2;

        // This would cast rays only against colliders in layer 8.
        // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
        layerMask = ~layerMask;

        RaycastHit hit;
        Item targetItem = null;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 10.0f)) {
            GameObject go = hit.transform.gameObject;
            targetItem = go.GetComponent<Item>();
            if (targetItem != null) {
                Debug.DrawRay(cam.transform.position, cam.transform.forward * hit.distance, Color.magenta);
                Debug.Log("Did Hit");
            }
        }
        if (pc.lastHit != null) {
            targetItem = pc.lastHit.collider.gameObject.GetComponent<Item>();
            if (targetItem != null) {
                if (!actionSlots.contents.ContainsKey("useItem")) {
                    Debug.Log("Picked Up");
                    actionSlots.AddItem("useItem", targetItem);
                }
            }
        }
        if(im.GetPickUp() && targetItem != null) {
            actionSlots.AddItem("useItem", targetItem);
        }
        if(im.GetDropItem() && actionSlots.contents.ContainsKey("useItem")) {
            actionSlots.RemoveItem("useItem");
        }
        if (im.GetUseItem() && actionSlots.contents.ContainsKey("useItem")) {
            Debug.Log("Item Used");
            actionSlots.contents["useItem"].ActionList["use"]("jump");
        }
    }
}
