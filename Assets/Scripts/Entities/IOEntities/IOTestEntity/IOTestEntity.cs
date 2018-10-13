using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("IOEntities/IOButton")]
public class IOTest : IOEntity, IUsable
{
    public DigitalState pressed;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(WaitThenChange());
    }

    // Update is called once per frame
    void Update()
    {
        return;
    }

    public void Use() {
        pressed.trigger();
        return;
    }

    public void TestChange(DigitalState input) {
        Debug.Log("Current state: " + input.state.ToString());
    }

    public IEnumerator WaitThenChange() {
        Debug.Log("Waiting to use...");
        yield return new WaitForSeconds(3);
        Use();
        Debug.Log("Waiting to set pressed...");
        yield return new WaitForSeconds(3);
        pressed.state = true;
        Debug.Log("Waiting to set pressed...");
        yield return new WaitForSeconds(3);
        pressed.state = false;
    }
}
