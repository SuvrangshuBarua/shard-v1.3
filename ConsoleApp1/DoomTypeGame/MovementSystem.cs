using SDL2;
using Shard;
using Shard.Shard;
using System;
using System.IO;
using System.Linq;
using System.Numerics;

namespace DoomTypeGame
{
    internal enum InputEventType
    {
        KeyDown = 0,
        KeyUp,
        MouseDown,
        JoyButtonDown,
        JoyButtonUp,
        JoyAxisMotion,
        MouseMotion
    }

    internal enum KeyMap
    {
        MoveForward,
        MoveBackward,
        MoveLeft,
        MoveRight,
        RotateClockwise,
        RotateCounterClockwise,
        Sprint,
        Shoot,
        Mute,
        VolumeUp,
        VolumeDown,
        Interact
    }

    internal class MovementSystem : InputListener
    {
        private readonly InputBindingSystem inputBindingSystem;
        private readonly DoomSoundSystem soundSystem;
        private IntPtr joystick;

        public MovementSystem()
        {
            inputBindingSystem = new InputBindingSystem();
            soundSystem = new DoomSoundSystem();
            soundSystem.Initialize();

            if (SDL.SDL_NumJoysticks() > 0)
            {
                SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING);
                joystick = SDL.SDL_JoystickOpen(0);
                if (joystick == IntPtr.Zero)
                {
                    Console.WriteLine("Unable to open joystick!");
                }
            }

            if (!File.Exists("bindings.json"))
            {
                inputBindingSystem.SaveBindings("bindings.json");
            }
            else
            {
                inputBindingSystem.LoadBindings("bindings.json");
            }
        }

        public void HandlePlayerMovement(Player player)
        {
            float speed = inputBindingSystem.GetInputValue(KeyMap.Sprint) > 0.5f ? player.RunSpeed : player.WalkSpeed;
            player.MyBody.MaxForce = speed;

            // Movement
            Vector2 moveDir = Vector2.Zero;
            float forwardValue = inputBindingSystem.GetInputValue(KeyMap.MoveForward);
            float backwardValue = inputBindingSystem.GetInputValue(KeyMap.MoveBackward);
            float leftValue = inputBindingSystem.GetInputValue(KeyMap.MoveLeft);
            float rightValue = inputBindingSystem.GetInputValue(KeyMap.MoveRight);

            moveDir += player.Transform.Forward * (forwardValue - backwardValue);
            moveDir += player.Transform.Right * (rightValue - leftValue);

            if (moveDir != Vector2.Zero)
            {
                moveDir = Vector2.Normalize(moveDir);
                player.MyBody.addForce(moveDir, speed);
            }

            // Rotation
            float rotateCWValue = inputBindingSystem.GetInputValue(KeyMap.RotateClockwise);
            float rotateCCWValue = inputBindingSystem.GetInputValue(KeyMap.RotateCounterClockwise);
            float netRotation = rotateCWValue - rotateCCWValue;
            player.MyBody.addTorque(netRotation * player.RotationSpeed);

            // Shooting and interacting
            if (inputBindingSystem.GetInputValue(KeyMap.Shoot) > 0.5f)
                player.startShooting();
            else
                player.stopShooting();

            if (inputBindingSystem.GetInputValue(KeyMap.Interact) > 0.5f)
                player.startInteracting();
            else
                player.stopInteracting();

            // Sound UI
            if (soundSystem.showMuteIndicator)
            {
                string muteText = soundSystem.isMuted ? "Muted" : "Unmuted";
                Bootstrap.getDisplay().showText(muteText, 1101, 21, 32, 255, 0, 0);
                Bootstrap.getDisplay().showText(muteText, 1100, 20, 32, 255, 255, 255);
                TimeSpan elapsed = DateTime.Now - soundSystem.muteIndicatorStartTime;
                if (elapsed.TotalSeconds >= 1.0)
                    soundSystem.showMuteIndicator = false;
            }

            // Rebinding UI
            if (inputBindingSystem.IsRebinding)
            {
                Bootstrap.getDisplay().showText(inputBindingSystem.RebindPromptText, 301, 251, 32, 255, 0, 0);
                Bootstrap.getDisplay().showText(inputBindingSystem.RebindPromptText, 300, 250, 32, 255, 255, 255);
            }
            else if (DateTime.Now < inputBindingSystem.RebindResultExpirationTime)
            {
                Bootstrap.getDisplay().showText(inputBindingSystem.RebindResultText, 301, 251, 32, 255, 0, 0);
                Bootstrap.getDisplay().showText(inputBindingSystem.RebindResultText, 300, 250, 32, 255, 255, 255);
            }
            else
            {
                Bootstrap.getDisplay().showText("", 300, 250, 32, 255, 0, 0);
            }
        }

        public void handleInput(InputEvent inp, string eventType)
        {
            inputBindingSystem.HandleInputEvent(inp, eventType);

            // Check for rebinding trigger (CTRL + bound key)
            if (eventType == "KeyDown" && inputBindingSystem.ActiveKeys.Contains((int)SDL.SDL_Scancode.SDL_SCANCODE_LCTRL))
            {
                foreach (var kvp in inputBindingSystem.InputBindings)
                {
                    if (kvp.Value.KeyboardScancode.HasValue && inputBindingSystem.ActiveKeys.Contains(kvp.Value.KeyboardScancode.Value))
                    {
                        inputBindingSystem.StartRebinding(kvp.Key);
                        break;
                    }
                }
            }

            // Handle sound control on press
            if (eventType == "KeyDown" || eventType == "JoyButtonDown")
            {
                bool isKeyDown = eventType == "KeyDown";
                int inputId = isKeyDown ? inp.Key : inp.JoyButton;

                if (IsBindingTriggered(KeyMap.Mute, isKeyDown, inputId))
                {
                    soundSystem.ToggleMute();
                }
                else if (IsBindingTriggered(KeyMap.VolumeUp, isKeyDown, inputId))
                {
                    soundSystem.IncreaseVolume();
                }
                else if (IsBindingTriggered(KeyMap.VolumeDown, isKeyDown, inputId))
                {
                    soundSystem.DecreaseVolume();
                }
            }
        }

        private bool IsBindingTriggered(KeyMap action, bool isKeyDown, int inputId)
        {
            var binding = inputBindingSystem.InputBindings[action];
            if (isKeyDown && binding.KeyboardScancode.HasValue && binding.KeyboardScancode.Value == inputId)
                return true;
            if (!isKeyDown && binding.JoystickButton.HasValue && binding.JoystickButton.Value == inputId)
                return true;
            return false;
        }

        ~MovementSystem()
        {
            if (joystick != IntPtr.Zero)
                SDL.SDL_JoystickClose(joystick);
        }
    }
}