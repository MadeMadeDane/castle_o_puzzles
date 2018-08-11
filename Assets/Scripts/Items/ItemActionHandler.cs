using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemActionHandler : MonoBehaviour {
    public PlayerController pc;
    public InputManager im;
    public CameraController cam_controller;
    public Inventory actionSlots;
    public float explosive_rad = 1.0f;
    public float select_reach_dist = 20.0f;
    public bool enable_logs = false;

    private Item prevItem = null;
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
        if (cam_controller.GetViewMode() == ViewMode.Shooter) {
            Camera cam = cam_controller.controlled_camera;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, select_reach_dist)) {
                targetItem = ExplosiveSelect(hit.point);
                Debug.DrawRay(cam.transform.position, hit.point - cam.transform.position, Color.cyan, 1.0f);
            } else {
                targetItem = ExplosiveSelect(cam.transform.position + cam.transform.forward * select_reach_dist);
            }
        }
        if (prevItem != null && prevItem != targetItem) {
            //prevItem.gameObject.GetComponent<ParticleSystem>().Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.EmissionModule emitter = prevItem.gameObject.GetComponent<ParticleSystem>().emission;
            emitter.enabled = false;
            prevItem = null;
        }
        if (targetItem != null) {
            if(!targetItem.gameObject.GetComponent<ParticleSystem>().isPlaying) {
                targetItem.gameObject.GetComponent<ParticleSystem>().Play();
            }
            ParticleSystem.EmissionModule emitter = targetItem.gameObject.GetComponent<ParticleSystem>().emission;
            emitter.enabled = true;
            prevItem = targetItem;
        } 
        if (im.GetPickUp() && targetItem != null) {
            actionSlots.AddItem("useItem", targetItem);
        }
        if (im.GetDropItem() && actionSlots.contents.ContainsKey("useItem")) {
            actionSlots.RemoveItem("useItem");
        }
        if (im.GetUseItem() && actionSlots.contents.ContainsKey("useItem")) {
            if (enable_logs) {
                Debug.Log("Item Used");
            }
            actionSlots.contents["useItem"].ActionList["use"]("jump");
        }
    }

    Item ExplosiveSelect(Vector3 pos)
    {
        Collider[] colliders = Physics.OverlapSphere(pos, explosive_rad);
        IEnumerable<Item> gos_in_explosion = colliders
            .Select(x => x.GetComponent<Item>())
            .Where(x => x != null);
        return gos_in_explosion
            .OrderBy(x => (pos - x.transform.position).magnitude)
            .FirstOrDefault();
    }
}