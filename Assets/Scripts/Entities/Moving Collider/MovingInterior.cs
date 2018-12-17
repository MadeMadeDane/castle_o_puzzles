using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingInterior : MonoBehaviour {

    private void OnTriggerStay(Collider other)
    {
        //Debug.Log("Staying");
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            //Debug.Log("updating timer");
            player.StayInMovingInterior();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //Debug.Log("exiting");
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.ExitMovingInterior();
        }
    }
}
