using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InputManager : MonoBehaviour {
    [Header("Input settings")]
    public Vector2 mouse_sensitivity;
    public Vector2 controller_sensitivity;

    // General input state and constants
    private float mouse_multiplier;
    private float controller_multiplier;
    private float _input_vertical_axis;
    private float _input_horizontal_axis;
    private bool _input_jump_button;
    private bool _input_jump_buttondown;
    private float _input_scroll_axis;

    // Mouse state
    private Queue<Vector2> mouseQueue;
    private Vector2 mouseQueueAvg = Vector2.zero;
    private int mouseQueueCount = 4;

    // Use this for initialization
    void Start () {
        // Initial state
        mouseQueue = new Queue<Vector2>(Enumerable.Repeat<Vector2>(Vector2.zero, mouseQueueCount));

        mouse_multiplier = 100;
        controller_multiplier = 50;
        mouse_sensitivity = 2 * Vector2.one;
        controller_sensitivity = new Vector2(6, 3);

        _input_vertical_axis = 0f;
        _input_horizontal_axis = 0f;
        _input_jump_button = false;
        _input_jump_buttondown = false;
        _input_scroll_axis = 0f;
    }

    // Update is called once per frame
    private void Update()
    {
        UpdateInputs();
    }

    private void UpdateInputs()
    {
        _input_vertical_axis = Input.GetAxisRaw("Vertical");
        _input_horizontal_axis = Input.GetAxisRaw("Horizontal");
        _input_jump_button = Input.GetButton("Jump");
        _input_jump_buttondown = Input.GetButtonDown("Jump");
        _input_scroll_axis = Input.GetAxis("Mouse ScrollWheel");
        MouseUpdate();
    }

    private void MouseUpdate()
    {
        // Handle both mouse and gamepad at the same time
        Vector2 rotVecM = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")) * mouse_sensitivity * mouse_multiplier * Time.deltaTime;
        Vector2 rotVecC = new Vector2(
            Input.GetAxisRaw("Joy X"),
            Input.GetAxisRaw("Joy Y")) * controller_sensitivity * controller_multiplier * Time.deltaTime;
        Vector2 rotVec = rotVecM + rotVecC;

        // Use rolling average for mouse smoothing (unity sucks at mouse input)
        mouseQueueAvg = mouseQueueAvg + ((rotVec - mouseQueue.Dequeue()) / mouseQueueCount);
        mouseQueue.Enqueue(rotVec);
    }

    public Vector2 GetMouseMotion()
    {
        return mouseQueueAvg;
    }

    public Vector2 GetMove()
    {
        return new Vector2(_input_horizontal_axis, _input_vertical_axis);
    }

    public float GetMoveHorizontal()
    {
        return _input_horizontal_axis;
    }

    public float GetMoveVertical()
    {
        return _input_vertical_axis;
    }

    public bool GetJump()
    {
        return _input_jump_buttondown || (Mathf.Abs(_input_scroll_axis) > 0f);
    }

    public bool GetJumpHold()
    {
        return _input_jump_button;
    }
}
