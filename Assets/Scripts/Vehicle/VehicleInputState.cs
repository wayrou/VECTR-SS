using UnityEngine;

namespace GTX.Vehicle
{
    [System.Serializable]
    public struct VehicleInputState
    {
        [Range(-1f, 1f)] public float steer;
        [Range(0f, 1f)] public float throttle;
        [Range(0f, 1f)] public float brake;
        [Range(0f, 1f)] public float clutch;
        public bool handbrake;
        public bool boost;
        public bool shiftUp;
        public bool shiftDown;

        public static VehicleInputState FromUnityInput()
        {
            float steerAxis = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                steerAxis -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                steerAxis += 1f;
            }

            VehicleInputState input = new VehicleInputState
            {
                steer = steerAxis,
                throttle = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f,
                brake = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f,
                clutch = Input.GetKey(KeyCode.LeftShift) ? 1f : 0f,
                handbrake = Input.GetKey(KeyCode.Space),
                boost = Input.GetKey(KeyCode.F),
                shiftUp = Input.GetKeyDown(KeyCode.E),
                shiftDown = Input.GetKeyDown(KeyCode.Q)
            };

            input.steer = Mathf.Clamp(input.steer, -1f, 1f);
            return input;
        }
    }
}
