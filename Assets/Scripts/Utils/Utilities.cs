using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAPI.Components;

public class Utilities : UnitySingleton<Utilities> {
    private Dictionary<string, Timer> Timers = new Dictionary<string, Timer>();
    public Dictionary<Type, Component> player_data = new Dictionary<Type, Component>();
    public GameObject currentPlayer = null;

    private IEnumerator WaitUntilConditionCoroutine(Func<bool> check, Action action) {
        yield return new WaitUntil(check);
        action();
    }

    public Coroutine WaitUntilCondition(Func<bool> check, Action action) {
        return StartCoroutine(WaitUntilConditionCoroutine(check, action));
    }

    private IEnumerator RunOnNextTickCoroutine(Action action) {
        yield return new WaitForFixedUpdate();
        action();
    }

    public Coroutine RunOnNextTick(Action action) {
        return StartCoroutine(RunOnNextTickCoroutine(action));
    }

    private IEnumerator RunOnNextFrameCoroutine(Action action) {
        yield return new WaitForEndOfFrame();
        action();
    }

    public Coroutine RunOnNextFrame(Action action) {
        return StartCoroutine(RunOnNextFrameCoroutine(action));
    }

    private IEnumerator WaitAndRunCoroutine(float seconds, Action action) {
        yield return new WaitForSeconds(seconds);
        action();
    }

    public Coroutine WaitAndRun(float seconds, Action action) {
        return StartCoroutine(WaitAndRunCoroutine(seconds, action));
    }

    public List<T_FIELD> GetAllFieldsOfType<T_CLASS, T_FIELD>(T_CLASS ent) where T_FIELD : class {
        return ent.GetType().GetFields()
            .Select((field) => field.GetValue(ent))
            .Where((field) => field is T_FIELD)
            .Select((obj) => obj as T_FIELD).ToList();
    }

    public Timer GetTimer(string name) {
        Timer timer;
        Timers.TryGetValue(name, out timer);
        return timer;
    }

    public Timer CreateTimer(string name, float period) {
        Timer timer = GetTimer(name);
        if (timer == null) {
            timer = new Timer(period: period);
            Timers.Add(name, timer);
        }
        return timer;
    }

    public void RemoveTimer(string name) {
        Timers.Remove(name);
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

    public T[] RayCastExplosiveSelectAll<T>(Vector3 origin, Vector3 path, float radius) where T : class {
        RaycastHit hit;
        if (Physics.Raycast(origin, path, out hit, path.magnitude)) {
            return ExplosiveSelectAll<T>(hit.point, radius);
        }
        return ExplosiveSelectAll<T>(origin + path, radius);
    }

    public T[] RayCastExplosiveSelectAll<T>(Vector3 origin, Vector3 path, float radius, out GameObject gameObject) where T : class {
        RaycastHit hit;
        if (Physics.Raycast(origin, path, out hit, path.magnitude)) {
            return ExplosiveSelectAll<T>(hit.point, radius, out gameObject);
        }
        return ExplosiveSelectAll<T>(origin + path, radius, out gameObject);
    }

    public T[] ExplosiveSelectAll<T>(Vector3 position, float radius, out GameObject gameObject) where T : class {
        gameObject = null;
        Collider[] colliders = Physics.OverlapSphere(position, radius);
        Collider nearest_selected = colliders.OrderBy(x => (position - x.transform.position).magnitude)
                                             .Where(x => x.GetComponent<T>() != null)
                                             .FirstOrDefault();
        if (nearest_selected != null) {
            gameObject = nearest_selected.gameObject;
            return nearest_selected.GetComponents<T>();
        }
        return new T[0];
    }

    public T[] ExplosiveSelectAll<T>(Vector3 position, float radius) where T : class {
        Collider[] colliders = Physics.OverlapSphere(position, radius);
        Collider nearest_selected = colliders.OrderBy(x => (position - x.transform.position).magnitude)
                        .Where(x => x.GetComponent<T>() != null)
                        .FirstOrDefault();
        if (nearest_selected != null) {
            return nearest_selected.GetComponents<T>();
        }
        return new T[0];
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
    public T get<T>() where T:Component {
        if (!player_data.ContainsKey(typeof(T))) {
            if (currentPlayer == null) {
               currentPlayer = SpawnManager.GetLocalPlayerObject().gameObject;
            }
            if (currentPlayer == null) return null;
            Component component = currentPlayer.GetComponentInChildren<T>();
            if (component == null) return null;
            player_data[typeof(T)] = component;
        }
        return (T) player_data[typeof(T)];
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

// WARNING: Dynamic value subtraction breaks WebGL. Create specificly typed buffers like below
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

    public int Size() {
        return size;
    }
}
