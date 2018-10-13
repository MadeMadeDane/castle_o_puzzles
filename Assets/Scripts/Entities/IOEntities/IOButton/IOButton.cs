using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class is purely for testing IOEntities
[AddComponentMenu("IOEntities/IOTestEntity")]
public class IOTestEntity : IOEntity, IUsable
{
    public DigitalState exampleDigital;
    public AnalogState exampleAnalog;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(TestDigitalStates());
    }

    public void Use() {
        exampleDigital.trigger();
    }

    public void TestDigitalChange(DigitalState input) {
        Debug.Log("Current state: " + input.state.ToString());
    }

    public void TestAnalogChange(AnalogState input) {
        Debug.Log("Current state: " + input.state.ToString());
    }

    public IEnumerator TestDigitalStates() {
        Debug.Log("Testing digital states");
        Debug.Log("Waiting to use...");
        yield return new WaitForSeconds(3);
        Use();
        Debug.Log("Waiting to set example digital state to true...");
        yield return new WaitForSeconds(3);
        exampleDigital.state = true;
        Debug.Log("Waiting to set example digital state to false...");
        yield return new WaitForSeconds(3);
        exampleDigital.state = false;
        StartCoroutine(TestAnalogStates());
    }

    public IEnumerator TestAnalogStates() {
        Debug.Log("Testing analog states");
        Debug.Log("Waiting to set example analog state to 127.0");
        yield return new WaitForSeconds(3);
        exampleAnalog.state = 127;
        Debug.Log("Waiting to set example analog state to 0.0");
        yield return new WaitForSeconds(3);
        exampleAnalog.state = 0;
    }
}
