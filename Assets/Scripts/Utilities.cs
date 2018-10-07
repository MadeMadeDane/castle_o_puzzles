using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utilities : UnitySingleton<Utilities>
{
    private Dictionary<string, Timer> Timers = new Dictionary<string, Timer>();

    public Timer GetTimer(string name)
    {
        Timer timer;
        Timers.TryGetValue(name, out timer);
        return timer;
    }

    public Timer CreateTimer(string name, float period)
    {
        Timer timer = new Timer(period: period);
        Timers.Add(name, timer);
        return timer;
    }

    public bool CheckTimer(string name)
    {
        return Timers.ContainsKey(name) && Timers[name].done();
    }

    public float GetTimerPeriod(string name)
    {
        if (Timers.ContainsKey(name))
        {
            return Timers[name].getPeriod();
        }
        else
        {
            return 0f;
        }
    }

    public float GetTimerPercent(string name)
    {
        if (Timers.ContainsKey(name))
        {
            return Timers[name].getPercent();
        }
        else
        {
            return 0f;
        }
    }

    public float GetTimerTime(string name)
    {
        if (Timers.ContainsKey(name))
        {
            return Timers[name].getTime();
        }
        else
        {
            return 0f;
        }
    }

    public void ResetTimer(string name)
    {
        if (Timers.ContainsKey(name))
        {
            Timers[name].reset();
        }
    }

    public void SetTimerFinished(string name)
    {
        if (Timers.ContainsKey(name))
        {
            Timers[name].setFinished();
        }
    }

    private void IncrementTimers()
    {
        foreach (Timer timer in Timers.Values)
        {
            timer.tick(Time.deltaTime);
        }
    }

    public void FixedUpdate()
    {
        IncrementTimers();
    }
}


public class Timer
{
    private float period;
    private float time;

    public Timer(float period)
    {
        this.period = period;
        time = 0f;
    }

    public void tick(float deltaTime)
    {
        time = Mathf.Clamp(time + deltaTime, 0f, period);
    }

    public float getTime()
    {
        return time;
    }

    public float getPercent()
    {
        return time/period;
    }

    public float getPeriod()
    {
        return period;
    }

    public bool done()
    {
        return time >= period;
    }

    public void reset()
    {
        time = 0f;
    }

    public void setFinished()
    {
        time = period;
    }
}
