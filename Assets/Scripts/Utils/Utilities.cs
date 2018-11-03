using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Utilities : UnitySingleton<Utilities> {
    private Dictionary<string, Timer> Timers = new Dictionary<string, Timer>();

    private IEnumerator RunOnNextTickCoroutine(Action action) {
        yield return new WaitForFixedUpdate();
        action();
    }

    public void RunOnNextTick(Action action) {
        StartCoroutine(RunOnNextTickCoroutine(action));
    }

    private IEnumerator RunOnNextFrameCoroutine(Action action) {
        yield return new WaitForEndOfFrame();
        action();
    }

    public void RunOnNextFrame(Action action) {
        StartCoroutine(RunOnNextFrameCoroutine(action));
    }

    public Timer GetTimer(string name) {
        Timer timer;
        Timers.TryGetValue(name, out timer);
        return timer;
    }

    public Timer CreateTimer(string name, float period) {
        Timer timer = new Timer(period: period);
        Timers.Add(name, timer);
        return timer;
    }

    public bool CheckTimer(string name) {
        return Timers.ContainsKey(name) && Timers[name].done();
    }

    public float GetTimerPeriod(string name) {
        if (Timers.ContainsKey(name)) {
            return Timers[name].getPeriod();
        }
        else {
            return 0f;
        }
    }

    public float GetTimerPercent(string name) {
        if (Timers.ContainsKey(name)) {
            return Timers[name].getPercent();
        }
        else {
            return 0f;
        }
    }

    public float GetTimerTime(string name) {
        if (Timers.ContainsKey(name)) {
            return Timers[name].getTime();
        }
        else {
            return 0f;
        }
    }

    public void ResetTimer(string name) {
        if (Timers.ContainsKey(name)) {
            Timers[name].reset();
        }
    }

    public void SetTimerFinished(string name) {
        if (Timers.ContainsKey(name)) {
            Timers[name].setFinished();
        }
    }

    private void IncrementTimers() {
        foreach (Timer timer in Timers.Values) {
            timer.tick(Time.deltaTime);
        }
    }

    public T RayCastExplosiveSelect<T>(Vector3 origin, Vector3 path, float radius) where T : class {
        RaycastHit hit;
        if (Physics.Raycast(origin, path, out hit, path.magnitude)) {
            return ExplosiveSelect<T>(hit.point, radius);
        }
        return ExplosiveSelect<T>(origin + path, radius);
    }

    public T RayCastExplosiveSelect<T>(Vector3 origin, Vector3 path, float radius, out GameObject gameObject) where T : class {
        RaycastHit hit;
        if (Physics.Raycast(origin, path, out hit, path.magnitude)) {
            return ExplosiveSelect<T>(hit.point, radius, out gameObject);
        }
        return ExplosiveSelect<T>(origin + path, radius, out gameObject);
    }

    public T ExplosiveSelect<T>(Vector3 position, float radius, out GameObject gameObject) where T : class {
        gameObject = null;
        Collider[] colliders = Physics.OverlapSphere(position, radius);
        Collider nearest_selected = colliders.OrderBy(x => (position - x.transform.position).magnitude)
                                             .Where(x => x.GetComponent<T>() != null)
                                             .FirstOrDefault();
        if (nearest_selected != null) {
            gameObject = nearest_selected.gameObject;
            return nearest_selected.GetComponent<T>();
        }
        return null;
    }

    public T ExplosiveSelect<T>(Vector3 position, float radius) where T : class {
        Collider[] colliders = Physics.OverlapSphere(position, radius);
        IEnumerable<T> gos_in_explosion = colliders
            .OrderBy(x => (position - x.transform.position).magnitude)
            .Select(x => x.GetComponent<T>())
            .Where(x => x != null);
        return gos_in_explosion.FirstOrDefault();
    }

    private void FixedUpdate() {
        IncrementTimers();
    }
}


public class Timer {
    private float period;
    private float time;

    public Timer(float period) {
        this.period = period;
        time = 0f;
    }

    public void tick(float deltaTime) {
        time = Mathf.Clamp(time + deltaTime, 0f, period);
    }

    public float getTime() {
        return time;
    }

    public float getPercent() {
        return time / period;
    }

    public float getPeriod() {
        return period;
    }

    public bool done() {
        return time >= period;
    }

    public void reset() {
        time = 0f;
    }

    public void setFinished() {
        time = period;
    }
}

public class Buffer<T> {
    private Queue<T> queue;
    private T accum;
    private int size;

    public Buffer(int size) {
        this.size = size;
        queue = new Queue<T>(Enumerable.Repeat<T>(default(T), size));
        accum = default(T);
    }

    public T Accumulate(T value) {
        accum += (dynamic)value - (dynamic)queue.Dequeue();
        queue.Enqueue(value);
        return accum;
    }

    public void Clear() {
        queue = new Queue<T>(Enumerable.Repeat<T>(default(T), size));
        accum = default(T);
    }
}

public class FloatBuffer {
    private Queue<float> queue;
    private float accum;
    private int size;

    public FloatBuffer(int size) {
        this.size = size;
        queue = new Queue<float>(Enumerable.Repeat<float>(0f, size));
        accum = 0f;
    }

    public float Accumulate(float value) {
        accum += value - queue.Dequeue();
        queue.Enqueue(value);
        return accum;
    }

    public void Clear() {
        queue = new Queue<float>(Enumerable.Repeat<float>(0f, size));
        accum = 0f;
    }
}
