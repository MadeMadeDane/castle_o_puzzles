using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Button
{
    private string[] _button_names;

    public Button(string button_name)
    {
        _button_names = new string[] { button_name };
    }

    public Button(string[] button_names)
    {
        _button_names = button_names;
    }

    public bool down()
    {
        return _button_names.Any(name => Input.GetButtonDown(name));
    }

    public bool pressed()
    {
        return _button_names.Any(name => Input.GetButton(name));
    }

    public bool up()
    {
        return _button_names.Any(name => Input.GetButtonUp(name));
    }
}

public class InputManager : MonoBehaviour {
    [Header("Input settings")]
    public Vector2 mouse_sensitivity;
    public Vector2 controller_sensitivity;

    // General input state and constants
    private float mouse_multiplier;
    private float controller_multiplier;
    private float _input_vertical_axis;
    private float _input_horizontal_axis;
    private float _input_scroll_axis;
    private Dictionary<string, Button> _button_map;

    // Mouse state
    private Queue<Vector2> mouseQueue;
    private Vector2 mouseQueueAvg = Vector2.zero;
    private int mouseQueueCount = 1;

    // Use this for initialization
    void Start () {
        // Initial state
        mouseQueue = new Queue<Vector2>(Enumerable.Repeat<Vector2>(Vector2.zero, mouseQueueCount));

        mouse_multiplier = 1f;
        controller_multiplier = 50f;
        mouse_sensitivity = 1.45f * Vector2.one;
        controller_sensitivity = new Vector2(4, 2);

        _input_vertical_axis = 0f;
        _input_horizontal_axis = 0f;
        _input_scroll_axis = 0f;

        _button_map = new Dictionary<string, Button>() {
            { "jump_button",  new Button("Jump") },
            { "center_camera_button",  new Button("Center Camera") },
            { "toggle_view_button",  new Button("Toggle View") },
            { "use_item_button", new Button("Use Item") },
            { "pick_up_button", new Button("Pick Up") },
            { "drop_item_button", new Button("Drop Item") },
        };
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
        _input_scroll_axis = Input.GetAxis("Mouse ScrollWheel");
        MouseUpdate();
    }

    private void MouseUpdate()
    {
        // Handle both mouse and gamepad at the same time
        Vector2 rotVecM = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")) * mouse_sensitivity * mouse_multiplier;
        // Controller input is framerate independent, but the camera updates every frame. Scale by frame time. 
        Vector2 rotVecC = new Vector2(
            Input.GetAxis("Joy X"),
            Input.GetAxis("Joy Y")) * controller_sensitivity * controller_multiplier * Time.deltaTime;
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
        return _button_map["jump_button"].down() || (Mathf.Abs(_input_scroll_axis) > 0f);
    }

    public bool GetJumpHold()
    {
        return _button_map["jump_button"].pressed();
    }

    public bool GetCenterCamera()
    {
        return _button_map["center_camera_button"].down();
    }

    public bool GetCenterCameraHold()
    {
        return _button_map["center_camera_button"].pressed();
    }
    
    public bool GetCenterCameraRelease()
    {
        return _button_map["center_camera_button"].up();
    }

    public bool GetToggleView()
    {
        return _button_map["toggle_view_button"].down();
    }

    public bool GetToggleViewHold()
    {
        return _button_map["toggle_view_button"].pressed();
    }

    public bool GetUseItem()
    {
        return _button_map["use_item_button"].down();
    }

    public bool GetUseItemHold()
    {
        return _button_map["use_item_button"].pressed();
    }

    public bool GetPickUp()
    {
        return _button_map["pick_up_button"].down();
    }

    public bool GetPickUpHold()
    {
        return _button_map["pick_up_button"].pressed();
    }

    public bool GetDropItem()
    {
        return _button_map["drop_item_button"].down();
    }

    public bool GetDropItemHold()
    {
        return _button_map["drop_item_button"].pressed();
    }
}
