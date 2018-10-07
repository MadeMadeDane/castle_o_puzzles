using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour {
    public CameraController cam_controller;
    public ActionSlots actionSlots;
    public Sprite image;
    public float explosive_rad = 1.0f;
    public float select_reach_dist = 5.0f;
    public bool enable_logs = false;
    private InputManager im;
    private ItemCatalogue amazon;


    private ItemRequest prevItem = null;
    // Use this for initialization
    private void Awake() {
        im = InputManager.Instance;
        amazon = new ItemCatalogue();
    }

    void Start ()
    {
    }

    // Update is called once per frame
    void Update ()
    {

        RaycastHit hit;
        ItemRequest targetItem = null;
        if (cam_controller.GetViewMode() == ViewMode.Shooter) {
            Camera cam = cam_controller.controlled_camera;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, select_reach_dist)) {
                targetItem = ExplosiveSelect(hit.point);
                //Debug.DrawRay(cam.transform.position, hit.point - cam.transform.position, Color.cyan, 1.0f);
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
        Item shipped_item = amazon.RequestItem(request.item_name);

        shipped_item.ctx = this;
        shipped_item.Start();
        shipped_item.menu_form = image;
        if (UseItem.isUseItem(shipped_item)) {
            actionSlots.AddUseItem((UseItem) shipped_item);
        }
        if (AbilityItem.isAbilityItem(shipped_item)) {
            actionSlots.AddAbilityItem(actionSlots.ability_items.contents.Count, (AbilityItem) shipped_item);
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