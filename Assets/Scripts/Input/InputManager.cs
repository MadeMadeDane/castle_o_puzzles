using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Button {
    private string[] _button_names;

    public Button(string button_name) {
        _button_names = new string[] { button_name };
    }

    public Button(string[] button_names) {
        _button_names = button_names;
    }

    public bool down() {
        return _button_names.Any(name => Input.GetButtonDown(name));
    }

    public bool pressed() {
        return _button_names.Any(name => Input.GetButton(name));
    }

    public bool up() {
        return _button_names.Any(name => Input.GetButtonUp(name));
    }
}

public class Axis {
    private string _axis_name;
    private float prev_value;
    public Axis(string axis_name) {
        _axis_name = axis_name;
    }

    public void UpdatePreviousValue() {
        prev_value = Input.GetAxisRaw(_axis_name);
    }

    public bool positive() {
        return Input.GetAxisRaw(_axis_name) > 0;
    }

    public bool approaching_positive() {
        return Input.GetAxisRaw(_axis_name) > prev_value;
    }

    public bool leaving_positive() {
        return Input.GetAxisRaw(_axis_name) < prev_value;
    }

    public bool negative() {
        return Input.GetAxisRaw(_axis_name) < 0;
    }

    public bool approaching_negative() {
        return Input.GetAxisRaw(_axis_name) < prev_value;
    }

    public bool leaving_negative() {
        return Input.GetAxisRaw(_axis_name) > prev_value;
    }
}

public class InputManager : UnitySingleton<InputManager> {
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
    private Dictionary<string, Axis> _axis_map;

    // Mouse state
    private Queue<Vector2> mouseQueue;
    private Vector2 mouseQueueAvg = Vector2.zero;
    private int mouseQueueCount = 1;

    private void Awake() {
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
            { "crouch_button",  new Button("Crouch") },
            { "center_camera_button",  new Button("Center Camera") },
            { "toggle_view_button",  new Button("Toggle View") },
            { "shared_item_button", new Button("Shared Item") },
            { "use_button", new Button("Use") },
            { "drop_item_button", new Button("Drop Item") },
            { "ability_slot_1_button", new Button("Ability Slot 1") },
            { "ability_slot_2_button", new Button("Ability Slot 2") },
            { "ability_slot_3_button", new Button("Ability Slot 3") },
            { "ability_slot_4_button", new Button("Ability Slot 4") },
            { "start_button", new Button("Start") },
        };
        _axis_map = new Dictionary<string, Axis>() {
            { "ability_slot_4_1_axis" , new Axis("Ability Slot 4 1")},
            { "ability_slot_2_3_axis" , new Axis("Ability Slot 2 3")},
        };
    }

    // Update is called once per frame
    private void Update() {
        UpdateInputs();
    }

    private void UpdateInputs() {
        _input_vertical_axis = Input.GetAxisRaw("Vertical");
        _input_horizontal_axis = Input.GetAxisRaw("Horizontal");
        _input_scroll_axis = Input.GetAxis("Mouse ScrollWheel");
        MouseUpdate();
    }

    private void MouseUpdate() {
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

    public Vector2 GetMouseMotion() {
        return mouseQueueAvg;
    }

    public Vector2 GetMove() {
        return new Vector2(_input_horizontal_axis, _input_vertical_axis);
    }

    public float GetMoveHorizontal() {
        return _input_horizontal_axis;
    }

    public float GetMoveVertical() {
        return _input_vertical_axis;
    }

    public bool GetJump() {
        return _button_map["jump_button"].down() || (Mathf.Abs(_input_scroll_axis) > 0f);
    }

    public bool GetJumpHold() {
        return _button_map["jump_button"].pressed();
    }

    public bool GetCrouch() {
        return _button_map["crouch_button"].down();
    }

    public bool GetCrouchHold() {
        return _button_map["crouch_button"].pressed();
    }

    public bool GetCrouchRelease() {
        return _button_map["crouch_button"].up();
    }

    public bool GetCenterCamera() {
        return _button_map["center_camera_button"].down();
    }

    public bool GetCenterCameraHold() {
        return _button_map["center_camera_button"].pressed();
    }

    public bool GetCenterCameraRelease() {
        return _button_map["center_camera_button"].up();
    }

    public bool GetToggleView() {
        return _button_map["toggle_view_button"].down();
    }

    public bool GetToggleViewHold() {
        return _button_map["toggle_view_button"].pressed();
    }

    public bool GetSharedItem() {
        return _button_map["shared_item_button"].down();
    }

    public bool GetSharedItemHold() {
        return _button_map["shared_item_button"].pressed();
    }

    public bool GetUse() {
        return _button_map["use_button"].down();
    }

    public bool GetUseHold() {
        return _button_map["use_button"].pressed();
    }

    public bool GetUseRelease() {
        return _button_map["use_button"].up();
    }

    public bool GetDropItem() {
        return _button_map["drop_item_button"].down();
    }

    public bool GetDropItemHold() {
        return _button_map["drop_item_button"].pressed();
    }

    public bool GetStart() {
        return _button_map["start_button"].down();
    }

    public bool GetStartHold() {
        return _button_map["start_button"].pressed();
    }

    public bool GetAbilitySlot1() {
        return _button_map["ability_slot_1_button"].down() || _axis_map["ability_slot_4_1_axis"].approaching_positive();
    }

    public bool GetAbilitySlot1Hold() {
        return _button_map["ability_slot_1_button"].pressed() || _axis_map["ability_slot_2_3_axis"].positive();
    }

    public bool GetAbilitySlot2() {
        return _button_map["ability_slot_2_button"].down() || _axis_map["ability_slot_2_3_axis"].approaching_negative();
    }

    public bool GetAbilitySlot2Hold() {
        return _button_map["ability_slot_2_button"].pressed() || _axis_map["ability_slot_2_3_axis"].negative();
    }

    public bool GetAbilitySlot3() {
        return _button_map["ability_slot_3_button"].down() || _axis_map["ability_slot_2_3_axis"].approaching_positive();
    }

    public bool GetAbilitySlot3Hold() {
        return _button_map["ability_slot_3_button"].pressed() || _axis_map["ability_slot_2_3_axis"].positive();
    }

    public bool GetAbilitySlot4() {
        return _button_map["ability_slot_4_button"].down() || _axis_map["ability_slot_4_1_axis"].approaching_negative();
    }

    public bool GetAbilitySlot4Hold() {
        return _button_map["ability_slot_4_button"].pressed() || _axis_map["ability_slot_4_1_axis"].negative();
    }
}
