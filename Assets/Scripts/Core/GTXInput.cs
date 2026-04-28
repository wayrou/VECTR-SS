using UnityEngine;

namespace GTX.Core
{
    public static class GTXInput
    {
        private const float Deadzone = 0.18f;
        private static readonly bool[] previousButtons = new bool[20];

        public static float Axis(string axisName)
        {
            try
            {
                float value = Input.GetAxisRaw(axisName);
                return Mathf.Abs(value) >= Deadzone ? value : 0f;
            }
            catch (System.ArgumentException)
            {
                return 0f;
            }
        }

        public static float PositiveAxis(string axisName)
        {
            float value = Axis(axisName);
            if (value <= Deadzone)
            {
                return 0f;
            }

            return Mathf.Clamp01(value);
        }

        public static float NegativeAxis(string axisName)
        {
            float value = Axis(axisName);
            if (value >= -Deadzone)
            {
                return 0f;
            }

            return Mathf.Clamp01(-value);
        }

        public static bool Button(int joystickButton)
        {
            return Input.GetKey(JoystickButton(joystickButton));
        }

        public static bool ButtonDown(int joystickButton)
        {
            return Input.GetKeyDown(JoystickButton(joystickButton));
        }

        public static bool AxisPressed(string axisName, float threshold)
        {
            return Axis(axisName) >= threshold;
        }

        public static bool AxisPressedDown(string axisName, float threshold, int latchIndex)
        {
            bool pressed = AxisPressed(axisName, threshold);
            bool wasPressed = latchIndex >= 0 && latchIndex < previousButtons.Length && previousButtons[latchIndex];
            if (latchIndex >= 0 && latchIndex < previousButtons.Length)
            {
                previousButtons[latchIndex] = pressed;
            }

            return pressed && !wasPressed;
        }

        public static bool AxisNegativePressedDown(string axisName, float threshold, int latchIndex)
        {
            bool pressed = Axis(axisName) <= -threshold;
            bool wasPressed = latchIndex >= 0 && latchIndex < previousButtons.Length && previousButtons[latchIndex];
            if (latchIndex >= 0 && latchIndex < previousButtons.Length)
            {
                previousButtons[latchIndex] = pressed;
            }

            return pressed && !wasPressed;
        }

        public static float CombinedDigital(float negative, float positive)
        {
            return Mathf.Clamp(positive - negative, -1f, 1f);
        }

        private static KeyCode JoystickButton(int button)
        {
            return (KeyCode)((int)KeyCode.JoystickButton0 + Mathf.Clamp(button, 0, 19));
        }
    }
}
