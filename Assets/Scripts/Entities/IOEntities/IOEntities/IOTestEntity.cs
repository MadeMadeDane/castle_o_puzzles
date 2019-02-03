using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class is purely for testing IOEntities
[AddComponentMenu("IOEntities/IOTestEntity")]
public class IOTestEntity : IOEntity, IUsable {
    public bool run_tests = false;
    public DigitalState exampleDigital;
    public AnalogState exampleAnalog;

    // Start is called before the first frame update
    private void Start() {
        if (run_tests) {
            //StartCoroutine(TestDigitalStates());
            //StartCoroutine(TestAnalogStates());
            StartCoroutine(TestIOLoop());
        }
    }

    public void Use() {
        Debug.Log("Using IOTestEntity");
        exampleDigital.trigger();
    }

    public void TestDigitalChange(DigitalState input) {
        Debug.Log("Current state: " + input.state.ToString());
    }

    public void TestAnalogChange(AnalogState input) {
        Debug.Log("Current state: " + input.state.ToString());
        Debug.Log("Previous state: " + input.previous_state.ToString());
    }

    public void TestAnalogLoopChange(AnalogState input) {
        if (input.state < 25600) {
            input.state++;
        }
        else {
            Debug.Log("Current state: " + input.state.ToString());
            Debug.Log("Finished testing");
        }
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
        Debug.Log("Finished testing");
    }

    public IEnumerator TestAnalogStates() {
        Debug.Log("Testing analog states");
        Debug.Log("Waiting to set example analog state to 127.0");
        yield return new WaitForSeconds(3);
        exampleAnalog.state = 127;
        Debug.Log("Waiting to set example analog state to 0.0");
        yield return new WaitForSeconds(3);
        exampleAnalog.state = 0;
        Debug.Log("Finished testing");
    }

    // Hook exampleAnalog up to TestAnalogLoopChange to start an IO event loop
    public IEnumerator TestIOLoop() {
        Debug.Log("Testing IO loop");
        yield return new WaitForSeconds(3);
        exampleAnalog.state = 1;
    }
}
