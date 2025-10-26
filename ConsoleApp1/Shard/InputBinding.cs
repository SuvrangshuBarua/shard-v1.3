using DoomTypeGame;
using SDL2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shard
{
    internal struct InputBinding
    {
        public int? KeyboardScancode { get; set; }

        [JsonConverter(typeof(NullableTupleConverter))]
        public (int Axis, bool Inverted)? JoystickAxis { get; set; }

        public int? JoystickButton { get; set; }
    }

    internal class InputBindingSystem
    {
        private readonly Dictionary<KeyMap, InputBinding> inputBindings = new Dictionary<KeyMap, InputBinding>();
        private readonly HashSet<int> activeKeys = new HashSet<int>();
        private readonly HashSet<int> activeJoystickButtons = new HashSet<int>();
        private float[] axisValues = new float[6]; // Assuming 6 joystick axes

        private KeyMap? rebindingAction = null;
        public string RebindPromptText { get; private set; } = "";
        public string RebindResultText { get; private set; } = "";
        public DateTime RebindResultExpirationTime { get; private set; } = DateTime.MinValue;

        // Constants
        private const float ACTION_THRESHOLD = 0.5f;

        private const int JOYSTICK_DEADZONE = 8000;
        private const float MAX_JOY_AXIS_VALUE = 32768.0f;

        public InputBindingSystem()
        {
            inputBindings[KeyMap.MoveForward] = new InputBinding { KeyboardScancode = (int)SDL.SDL_Scancode.SDL_SCANCODE_W, JoystickAxis = (1, true) };
            inputBindings[KeyMap.MoveBackward] = new InputBinding { KeyboardScancode = (int)SDL.SDL_Scancode.SDL_SCANCODE_S };
            inputBindings[KeyMap.MoveLeft] = new InputBinding { KeyboardScancode = (int)SDL.SDL_Scancode.SDL_SCANCODE_A };
            inputBindings[KeyMap.MoveRight] = new InputBinding { KeyboardScancode = (int)SDL.SDL_Scancode.SDL_SCANCODE_D, JoystickAxis = (0, false) };
            inputBindings[KeyMap.RotateClockwise] = new InputBinding { KeyboardScancode = (int)SDL.SDL_Scancode.SDL_SCANCODE_E, JoystickAxis = (2, false) };
            inputBindings[KeyMap.RotateCounterClockwise] = new InputBinding { KeyboardScancode = (int)SDL.SDL_Scancode.SDL_SCANCODE_Q };
            inputBindings[KeyMap.Sprint] = new InputBinding { KeyboardScancode = (int)SDL.SDL_Scancode.SDL_SCANCODE_LSHIFT, JoystickButton = 4 };
            inputBindings[KeyMap.Shoot] = new InputBinding { KeyboardScancode = (int)SDL.SDL_Scancode.SDL_SCANCODE_SPACE, JoystickButton = 0 };
            inputBindings[KeyMap.Mute] = new InputBinding { KeyboardScancode = (int)SDL.SDL_Scancode.SDL_SCANCODE_M };
            inputBindings[KeyMap.VolumeUp] = new InputBinding { KeyboardScancode = (int)SDL.SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET };
            inputBindings[KeyMap.VolumeDown] = new InputBinding { KeyboardScancode = (int)SDL.SDL_Scancode.SDL_SCANCODE_LEFTBRACKET };
            inputBindings[KeyMap.Interact] = new InputBinding { KeyboardScancode = (int)SDL.SDL_Scancode.SDL_SCANCODE_I, JoystickButton = 2 };
        }

        public void HandleInputEvent(InputEvent inp, string eventType)
        {
            if (!Enum.TryParse(eventType, out InputEventType inputEventType)) return;

            switch (inputEventType)
            {
                case InputEventType.KeyDown:
                    activeKeys.Add(inp.Key);
                    if (rebindingAction.HasValue)
                        HandleRebinding(inp, eventType);
                    break;

                case InputEventType.KeyUp:
                    activeKeys.Remove(inp.Key);
                    break;

                case InputEventType.JoyButtonDown:
                    activeJoystickButtons.Add(inp.JoyButton);
                    if (rebindingAction.HasValue)
                        HandleRebinding(inp, eventType);
                    break;

                case InputEventType.JoyButtonUp:
                    activeJoystickButtons.Remove(inp.JoyButton);
                    break;

                case InputEventType.JoyAxisMotion:
                    if (inp.JoyAxis < axisValues.Length)
                        axisValues[inp.JoyAxis] = inp.JoyAxisValue;
                    if (rebindingAction.HasValue)
                        HandleRebinding(inp, eventType);
                    break;
            }
        }

        public float GetInputValue(KeyMap action)
        {
            if (!inputBindings.TryGetValue(action, out var binding))
                return 0.0f;

            if (binding.KeyboardScancode.HasValue && activeKeys.Contains(binding.KeyboardScancode.Value))
                return 1.0f;

            if (binding.JoystickButton.HasValue && activeJoystickButtons.Contains(binding.JoystickButton.Value))
                return 1.0f;

            if (binding.JoystickAxis.HasValue)
            {
                var (axis, inverted) = binding.JoystickAxis.Value;
                if (axis < axisValues.Length)
                {
                    float value = axisValues[axis];
                    if (inverted)
                        value = -value;
                    value = ApplyDeadZone(value);
                    return value;
                }
            }

            return 0.0f;
        }

        private float ApplyDeadZone(float value)
        {
            float absValue = Math.Abs(value);
            if (absValue < JOYSTICK_DEADZONE / MAX_JOY_AXIS_VALUE)
                return 0.0f;
            return value;
        }

        public void StartRebinding(KeyMap action)
        {
            rebindingAction = action;
            RebindPromptText = $"Rebinding {action}: Press a key, joystick button, or move an axis";
            RebindResultText = "";
        }

        private void HandleRebinding(InputEvent inp, string eventType)
        {
            if (eventType == "KeyDown" && inp.Key == (int)SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE)
            {
                FinishRebinding("Rebinding aborted");
                return;
            }

            var binding = inputBindings[rebindingAction.Value];
            string result = "";

            switch (eventType)
            {
                case "KeyDown":
                    binding.KeyboardScancode = inp.Key;

                    inputBindings[rebindingAction.Value] = binding;
                    result = $"{rebindingAction.Value} rebound to {SDL.SDL_GetKeyName(SDL.SDL_GetKeyFromScancode((SDL.SDL_Scancode)inp.Key))}";
                    FinishRebinding(result);
                    break;

                case "JoyButtonDown":
                    binding.JoystickButton = inp.JoyButton;

                    inputBindings[rebindingAction.Value] = binding;
                    result = $"{rebindingAction.Value} rebound to button {inp.JoyButton}";
                    FinishRebinding(result);
                    break;

                case "JoyAxisMotion":
                    if (Math.Abs(inp.JoyAxisValue) > 0.5f)
                    {
                        binding.JoystickAxis = (inp.JoyAxis, inp.JoyAxisValue < 0);

                        inputBindings[rebindingAction.Value] = binding;
                        string direction = inp.JoyAxisValue < 0 ? "negative" : "positive";
                        result = $"{rebindingAction.Value} rebound to axis {inp.JoyAxis} {direction}";
                        FinishRebinding(result);
                    }
                    break;
            }
        }

        private void FinishRebinding(string resultText)
        {
            if (resultText != "Rebinding aborted")
                SaveBindings("bindings.json");
            RebindPromptText = "";
            RebindResultText = resultText;
            RebindResultExpirationTime = DateTime.Now.AddSeconds(1.0);
            rebindingAction = null;
        }

        public void SaveBindings(string filePath)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new NullableTupleConverter() }
            };
            var json = JsonSerializer.Serialize(inputBindings, options);
            File.WriteAllText(filePath, json);
            Console.WriteLine($"Bindings saved to {filePath}");
        }

        public void LoadBindings(string filePath)
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    Converters = { new NullableTupleConverter() }
                };
                var loadedBindings = JsonSerializer.Deserialize<Dictionary<KeyMap, InputBinding>>(json, options);
                foreach (var kvp in loadedBindings)
                {
                    inputBindings[kvp.Key] = kvp.Value;
                }
                Console.WriteLine($"Bindings loaded from {filePath}");
            }
        }

        public bool IsRebinding => rebindingAction.HasValue;

        public IReadOnlyDictionary<KeyMap, InputBinding> InputBindings => inputBindings;

        public IReadOnlyCollection<int> ActiveKeys => activeKeys;
    }
}