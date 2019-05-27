using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MLAPI;

// This class tracks IOEntities in order to provide connection information
public class IOEntity : NetworkedBehaviour {
    public HashSet<DigitalState> ConnectedDigitalInputs = new HashSet<DigitalState>();
    public HashSet<AnalogState> ConnectedAnalogInputs = new HashSet<AnalogState>();
    public HashSet<VectorState> ConnectedVectorInputs = new HashSet<VectorState>();
    public HashSet<DigitalState> DigitalOutputs = new HashSet<DigitalState>();
    public HashSet<AnalogState> AnalogOutputs = new HashSet<AnalogState>();
    public HashSet<VectorState> VectorOutputs = new HashSet<VectorState>();
    protected Utilities utils;

    private void Awake() {
        utils = Utilities.Instance;
        // Use reflection to determine which IOEntities we are hooked up to
        IndexDigitalConnections();
        IndexAnalogConnections();
        IndexVectorConnections();
        Startup();
    }

    // Use this function instead of awake when creating an IOEntity
    protected virtual void Startup() { }

    protected void IndexDigitalConnections() {
        // Get all digital states on this IOEntity
        List<DigitalState> myDigitalStates = utils.GetAllFieldsOfType<IOEntity, DigitalState>(this);
        foreach (DigitalState dState in myDigitalStates) {
            DigitalOutputs.Add(dState);
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
            AnalogOutputs.Add(aState);
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

    protected void IndexVectorConnections() {
        List<VectorState> myVectorStates = utils.GetAllFieldsOfType<IOEntity, VectorState>(this);
        foreach (VectorState vState in myVectorStates) {
            VectorOutputs.Add(vState);
            // Find every analog state change event on that specific analog state
            foreach (VectorStateChange vChange in utils.GetAllFieldsOfType<VectorState, VectorStateChange>(vState)) {
                // Find every listener on that specific analog state change
                foreach (int idx in Enumerable.Range(0, vChange.GetPersistentEventCount())) {
                    // Add myself to the targets list of analog input connections
                    Object target = vChange.GetPersistentTarget(idx);
                    if (!(target is IOEntity)) continue;
                    (target as IOEntity).ConnectedVectorInputs.Add(vState);
                }
            }
        }
    }
}
