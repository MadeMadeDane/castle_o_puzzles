using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// This class tracks IOEntities in order to provide connection information
public class IOEntity : MonoBehaviour {
    public HashSet<DigitalState> ConnectedDigitalInputs = new HashSet<DigitalState>();
    public HashSet<AnalogState> ConnectedAnalogInputs = new HashSet<AnalogState>();
    Utilities utils;

    protected virtual void Awake() {
        utils = Utilities.Instance;
        // Use reflection to determine which IOEntities we are hooked up to
        IndexDigitalConnections();
        IndexAnalogConnections();
    }

    protected void IndexDigitalConnections() {
        // Get all digital states on this IOEntity
        List<DigitalState> myDigitalStates = utils.GetAllFieldsOfType<IOEntity, DigitalState>(this);
        foreach (DigitalState dState in myDigitalStates) {
            // Find every digital state change event on that specific digital state
            foreach (DigitalStateChange dChange in utils.GetAllFieldsOfType<DigitalState, DigitalStateChange>(dState)) {
                // Find every listener on that specific digital state change
                foreach (int idx in Enumerable.Range(0, dChange.GetPersistentEventCount())) {
                    // Add myself to the targets list of digital input connections
                    Object target = dChange.GetPersistentTarget(idx);
                    if (!(target is IOEntity)) continue;
                    (target as IOEntity).ConnectedDigitalInputs.Add(dState);
                }
            }
        }
    }

    protected void IndexAnalogConnections() {
        List<AnalogState> myAnalogStates = utils.GetAllFieldsOfType<IOEntity, AnalogState>(this);
        foreach (AnalogState aState in myAnalogStates) {
            // Find every analog state change event on that specific analog state
            foreach (AnalogStateChange aChange in utils.GetAllFieldsOfType<AnalogState, AnalogStateChange>(aState)) {
                // Find every listener on that specific analog state change
                foreach (int idx in Enumerable.Range(0, aChange.GetPersistentEventCount())) {
                    // Add myself to the targets list of analog input connections
                    Object target = aChange.GetPersistentTarget(idx);
                    if (!(target is IOEntity)) continue;
                    (target as IOEntity).ConnectedAnalogInputs.Add(aState);
                }
            }
        }
    }
}
