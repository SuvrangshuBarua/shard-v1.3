/*
*
*   This is a general, simple container for all the information someone might want to know about
*       keyboard or mouse input.   The same object is used for both, so use your common sense
*       to work out whether you can use the contents of, say 'x' and 'y' when registering for
*       a key event.
*   @author Michael Heron
*   @version 1.0
*
*/

namespace Shard
{
    internal class InputEvent
    {
        private int x;
        private int y;
        private int deltaX;
        private int deltaY;
        private int button;
        private int key;

        private string classification;

        // New joystick properties
        private int joystickID;

        private int joyButton;
        private int joyAxis;
        private float joyAxisValue;

        public int X
        {
            get => x;
            set => x = value;
        }

        public int Y
        {
            get => y;
            set => y = value;
        }

        public int DeltaX
        {
            get => deltaX;
            set => deltaX = value;
        }

        public int DeltaY
        {
            get => deltaY;
            set => deltaY = value;
        }

        public int Button
        {
            get => button;
            set => button = value;
        }

        public string Classification
        {
            get => classification;
            set => classification = value;
        }

        public int Key
        {
            get => key;
            set => key = value;
        }

        // Joystick properties
        public int JoystickID
        {
            get => joystickID;
            set => joystickID = value;
        }

        public int JoyButton
        {
            get => joyButton;
            set => joyButton = value;
        }

        public int JoyAxis
        {
            get => joyAxis;
            set => joyAxis = value;
        }

        public float JoyAxisValue
        {
            get => joyAxisValue;
            set => joyAxisValue = value;
        }
    }
}