using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionRecovery : MonoBehaviour {

	// Use this for initialization
	void Start () {
        Collider my_collider = GetComponent<Collider>();
		foreach (Collider col in GetComponentsInParent<Collider>())
        {
            Physics.IgnoreCollision(my_collider, col);
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void OnTriggerStay(Collider other)
    {
        if (!other.isTrigger)
        {
            Debug.Log("hi");
        }
    }
}
