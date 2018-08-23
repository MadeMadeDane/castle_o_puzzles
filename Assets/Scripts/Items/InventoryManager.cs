using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour {
    public PlayerController pc;
    public InputManager im;
    public CameraController cam_controller;
    public ActionSlots actionSlots;
    public Sprite image;
    public float explosive_rad = 1.0f;
    public float select_reach_dist = 20.0f;
    public bool enable_logs = false;
    private ItemCatalogue amazon;


    private ItemRequest prevItem = null;
    // Use this for initialization
    void Start ()
    {
        amazon = new ItemCatalogue();
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
        ItemRequest targetItem = null;
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
            AddItemToInventory(targetItem);
        }
        if (im.GetDropItem() && actionSlots.use_item != null) {
            actionSlots.DropItem();
        }
    }
    void FixedUpdate()
    {
        
    }

    void AddItemToInventory (ItemRequest request)
    {
        Item shipped_item = amazon.RequestItem(request);

        shipped_item.ctx = this;
        shipped_item.Start();
        shipped_item.menu_form = image;
        Debug.Log("Adding item");
        if (UseItem.isUseItem(shipped_item)) {
            Debug.Log("Use item");
            actionSlots.AddUseItem((UseItem) shipped_item);
        }
        if (AbilityItem.isAbilityItem(shipped_item)) {
            Debug.Log("Ability item");
            actionSlots.AddAbilityItem(actionSlots.ability_items.contents.Count + 1, (AbilityItem) shipped_item);
        }
        GameObject.Destroy(request.gameObject);
    }

    ItemRequest ExplosiveSelect(Vector3 pos)
    {
        Collider[] colliders = Physics.OverlapSphere(pos, explosive_rad);
        IEnumerable<ItemRequest> gos_in_explosion = colliders
            .Select(x => x.GetComponent<ItemRequest>())
            .Where(x => x != null);
        return gos_in_explosion
            .OrderBy(x => (pos - x.transform.position).magnitude)
            .FirstOrDefault();
    }

}