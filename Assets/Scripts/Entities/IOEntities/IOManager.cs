using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class IOManager : UnitySingleton<IOManager> {
    private int CurrentIOTick = 0;
    // At a game tick rate of 250hz our IOTickRate is 1000hz
    private const int IOTickRate = 4;

    // How this works:
    //  An IO event chain is started by an action running. If that action triggers another action the
    //  current tick is incremented. If this chain continues the CurrentIOTick will keep being incremented
    //  without being set back to 0. When it reaches the IOTickRate the next action in the chain is scheduled
    //  to run on the next game tick, thus breaking the chain and setting the CurrentIOTick back to 0.
    //  The IOTickRate represents the maximum length of any IO event chain allowed to run in a game tick.
    public void IOTick(Action action) {
        // Prevent all IO from flowing if we are not the server
        if (!NetworkingManager.Singleton.IsServer) return;
        if (CurrentIOTick >= IOTickRate) {
            Utilities.Instance.RunOnNextTick(() => IOTick(action));
        }
        else {
            CurrentIOTick++;
            action();
        }
        CurrentIOTick = 0;
    }
}