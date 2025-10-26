/*
*
*   SDL provides an input layer, and we're using that.  This class tracks input, anchors it to the
*       timing of the game loop, and converts the SDL events into one that is more abstract so games
*       can be written more interchangeably.
*   @author Michael Heron
*   @version 1.0
*
*/

using SDL2;
using System;
using System.Collections.Generic;

namespace Shard
{
    internal class InputFramework : InputSystem
    {
        private Dictionary<int, IntPtr> connectedJoysticks = new Dictionary<int, IntPtr>();

        public override void getInput()
        {
            SDL.SDL_Event ev;
            InputEvent ie;

            // Poll events continuously every frame.
            while (SDL.SDL_PollEvent(out ev) != 0)
            {
                ie = new InputEvent();

                if (ev.type == SDL.SDL_EventType.SDL_MOUSEMOTION)
                {
                    SDL.SDL_MouseMotionEvent mot = ev.motion;
                    ie.X = mot.x;
                    ie.Y = mot.y;
                    ie.DeltaX = -mot.xrel;
                    ie.DeltaY = mot.yrel;
                    informListeners(ie, "MouseMotion");
                }
                else if (ev.type == SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN)
                {
                    SDL.SDL_MouseButtonEvent butt = ev.button;
                    ie.Button = (int)butt.button;
                    ie.X = butt.x;
                    ie.Y = butt.y;
                    informListeners(ie, "MouseDown");
                }
                else if (ev.type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP)
                {
                    SDL.SDL_MouseButtonEvent butt = ev.button;
                    ie.Button = (int)butt.button;
                    ie.X = butt.x;
                    ie.Y = butt.y;
                    informListeners(ie, "MouseUp");
                }
                else if (ev.type == SDL.SDL_EventType.SDL_MOUSEWHEEL)
                {
                    SDL.SDL_MouseWheelEvent wh = ev.wheel;
                    ie.X = (int)wh.direction * wh.x;
                    ie.Y = (int)wh.direction * wh.y;
                    informListeners(ie, "MouseWheel");
                }
                else if (ev.type == SDL.SDL_EventType.SDL_KEYDOWN)
                {
                    ie.Key = (int)ev.key.keysym.scancode;
                    Debug.getInstance().log("KeyDown: " + ie.Key);
                    informListeners(ie, "KeyDown");
                }
                else if (ev.type == SDL.SDL_EventType.SDL_KEYUP)
                {
                    ie.Key = (int)ev.key.keysym.scancode;
                    informListeners(ie, "KeyUp");
                }
                else if (ev.type == SDL.SDL_EventType.SDL_JOYAXISMOTION)
                {
                    ie.JoystickID = ev.jaxis.which;
                    ie.JoyAxis = ev.jaxis.axis;

                    // Normalize the axis value to the range [-1.0, 1.0]
                    float normalizedValue = ev.jaxis.axisValue / 32768.0f;

                    // Apply a deadzone; adjust the threshold if needed.
                    if (MathF.Abs(ev.jaxis.axisValue) < 8000)
                    {
                        normalizedValue = 0;
                    }

                    ie.JoyAxisValue = normalizedValue;
                    //Debug.getInstance().log($"JoyAxis: {ie.JoyAxis} Value: " + ie.JoyAxisValue);
                    informListeners(ie, "JoyAxisMotion");
                }
                else if (ev.type == SDL.SDL_EventType.SDL_JOYBUTTONDOWN)
                {
                    ie.JoystickID = ev.jbutton.which;
                    ie.JoyButton = ev.jbutton.button;
                    //Debug.getInstance().log("JoyButtonDown: " + ie.JoyButton);
                    informListeners(ie, "JoyButtonDown");
                }
                else if (ev.type == SDL.SDL_EventType.SDL_JOYBUTTONUP)
                {
                    ie.JoystickID = ev.jbutton.which;
                    ie.JoyButton = ev.jbutton.button;
                    //Debug.getInstance().log("JoyButtonUp: " + ie.JoyButton);
                    informListeners(ie, "JoyButtonUp");
                }
                else if (ev.type == SDL.SDL_EventType.SDL_JOYDEVICEADDED)
                {
                    int deviceIndex = ev.jdevice.which;
                    IntPtr joystick = SDL.SDL_JoystickOpen(deviceIndex);
                    if (joystick != IntPtr.Zero)
                    {
                        int instanceID = SDL.SDL_JoystickInstanceID(joystick);
                        //Debug.getInstance().log("JoyAdded ID: " + joystick.ToString());
                        connectedJoysticks[instanceID] = joystick;
                    }
                }
                else if (ev.type == SDL.SDL_EventType.SDL_JOYDEVICEREMOVED)
                {
                    int instanceID = ev.jdevice.which;
                    if (connectedJoysticks.TryGetValue(instanceID, out IntPtr joystick))
                    {
                        //Debug.getInstance().log("JoyRemoved ID: " + joystick.ToString());
                        SDL.SDL_JoystickClose(joystick);
                        connectedJoysticks.Remove(instanceID);
                    }
                }
            }
        }

        public override void initialize()
        {
            SDL.SDL_InitSubSystem(SDL.SDL_INIT_JOYSTICK);
        }

        ~InputFramework()
        {
            // Clean up all joysticks
            foreach (var joystick in connectedJoysticks.Values)
            {
                SDL.SDL_JoystickClose(joystick);
            }
            connectedJoysticks.Clear();
            SDL.SDL_QuitSubSystem(SDL.SDL_INIT_JOYSTICK);
        }
    }
}