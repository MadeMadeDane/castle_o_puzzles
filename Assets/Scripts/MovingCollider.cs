using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MovingCollider : MovingGeneric {
    public GameObject nextTargetObject;
    private Vector3 target_velocity;
    private float distanceThreshold;
    private GameObject home;

    // Use this for initialization
    void Start () {
        velocity = Vector3.zero;
        target_velocity = Vector3.zero;
        distanceThreshold = 0.1f;
        // Save the original target as the home
        home = nextTargetObject;
        StartCoroutine(MoveToNextTarget());
    }
	
	// Update is called once per frame
	void Update () {
	}

    private void FixedUpdate()
    {
        velocity = Vector3.Lerp(velocity, target_velocity, 0.1f);
        transform.Translate(velocity*Time.deltaTime);
    }

    private IEnumerator MoveToNextTarget()
    {
        while (true)
        {
            if (nextTargetObject == null)
            {
                // Wait for nextTargetObject to bet set
                yield return new WaitUntil(() => nextTargetObject != null);
            }
            MotionTarget nextTarget = nextTargetObject.GetComponent<MotionTarget>();
            // Reset to home if we fail to find a next target
            if (nextTarget == null)
            {
                Debug.Log("Target was not a MotionTarget");
                nextTargetObject = home;
                yield return new WaitForSeconds(1f);
                continue;
            }

            while ((nextTarget.transform.position - transform.position).magnitude > distanceThreshold)
            {
                target_velocity = (nextTarget.transform.position - transform.position).normalized * nextTarget.speed;
                // Synchronize with fixed update
                yield return new WaitForFixedUpdate();
            }

            // Put the coroutine to sleep while we wait
            target_velocity = Vector3.zero;
            if (nextTarget.waitTime != 0f)
            {
                yield return new WaitForSeconds(nextTarget.waitTime);
            }
            nextTargetObject = nextTarget.nextTargetObject;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        Debug.Log("Collision: " + (from contact in collision.contacts select contact.normal).ToString());
    }
}
