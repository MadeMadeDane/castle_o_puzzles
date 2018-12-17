using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using MLAPI;

public class PhysicsPropHandler : NetworkedBehaviour {
    private Dictionary<Type, PhysicsPlugin> plugins;

    private void Setup() {
        plugins = new Dictionary<Type, PhysicsPlugin>() {
            {typeof(Pushable), new Pusher(context: this)},
            {typeof(Grabable), new Grabber(context: this)}
        };

        foreach (ComponentPlugin plugin in plugins.Values) {
            plugin.Awake();
        }
    }

    public T GetPlugin<T>() where T : PhysicsPlugin {
        return plugins.Values.Where(x => x is T).FirstOrDefault() as T;
    }

    public PhysicsPlugin GetPluginByProp<T>() where T : PhysicsProp {
        PhysicsPlugin plugin;
        plugins.TryGetValue(typeof(T), out plugin);
        return plugin;
    }

    private void Start() {
        if (!isOwner) return;
        Setup();
        foreach (ComponentPlugin plugin in plugins.Values) {
            plugin.Start();
        }
    }

    private void Update() {
        if (!isOwner) return;
        foreach (ComponentPlugin plugin in plugins.Values) {
            plugin.Update();
        }
    }

    private void FixedUpdate() {
        if (!isOwner) return;
        foreach (ComponentPlugin plugin in plugins.Values) {
            plugin.FixedUpdate();
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!isOwner) return;
        PhysicsProp[] props = other.GetComponents<PhysicsProp>();
        foreach (PhysicsProp prop in props) {
            plugins[prop.GetType()].OnTriggerEnter(other, prop);
        }
    }

    private void OnTriggerStay(Collider other) {
        if (!isOwner) return;
        PhysicsProp[] props = other.GetComponents<PhysicsProp>();
        foreach (PhysicsProp prop in props) {
            plugins[prop.GetType()].OnTriggerStay(other, prop);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (!isOwner) return;
        PhysicsProp[] props = other.GetComponents<PhysicsProp>();
        foreach (PhysicsProp prop in props) {
            plugins[prop.GetType()].OnTriggerExit(other, prop);
        }
    }

    private void OnCollisionEnter(Collision other) {
        if (!isOwner) return;
        PhysicsProp[] props = other.gameObject.GetComponents<PhysicsProp>();
        foreach (PhysicsProp prop in props) {
            plugins[prop.GetType()].OnCollisionEnter(other, prop);
        }
    }

    private void OnCollisionStay(Collision other) {
        if (!isOwner) return;
        PhysicsProp[] props = other.gameObject.GetComponents<PhysicsProp>();
        foreach (PhysicsProp prop in props) {
            plugins[prop.GetType()].OnCollisionStay(other, prop);
        }
    }

    private void OnCollisionExit(Collision other) {
        if (!isOwner) return;
        PhysicsProp[] props = other.gameObject.GetComponents<PhysicsProp>();
        foreach (PhysicsProp prop in props) {
            plugins[prop.GetType()].OnCollisionExit(other, prop);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit) {
        if (!isOwner) return;
        PhysicsProp[] props = hit.gameObject.GetComponents<PhysicsProp>();
        foreach (PhysicsProp prop in props) {
            plugins[prop.GetType()].OnControllerColliderHit(hit, prop);
        }
    }

    public void HandleUse(GameObject other) {
        PhysicsProp[] props = other.GetComponents<PhysicsProp>();
        foreach (PhysicsProp prop in props) {
            plugins[prop.GetType()].OnUse(prop);
        }
    }

    public bool IsServer() {
        return isServer;
    }
}
