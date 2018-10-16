using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IOManager : UnitySingleton<IOManager>
{
    private int CurrentIOTick = 0;
    // At a game tick rate of 250hz our IOTickRate is 1000hz
    private const int IOTickRate = 4;

    private IEnumerator RunOnNextTickCoroutine(Action action) {
        yield return new WaitForFixedUpdate();
        action();
    }

    public void RunOnNextTick(Action action) {
        StartCoroutine(RunOnNextTickCoroutine(action));
    }

    // How this works:
    //  An IO event chain is started by an action running. If that action triggers another action the
    //  current tick is incremented. If this chain continues the CurrentIOTick will keep being incremented
    //  without being set back to 1. When it reaches the IOTickRate the next action in the chain is scheduled
    //  to run on the next game tick, thus breaking the chain and setting the CurrentIOTick back to 1.
    //  The IOTickRate represents the maximum length of any IO event chain allowed to run in a game tick.
    public void IOTick(Action action) {
        if (CurrentIOTick >= IOTickRate) {
            RunOnNextTick(() => IOTick(action));
        }
        else {
            CurrentIOTick++;
            action();
        }
        CurrentIOTick = 0;
    }
}