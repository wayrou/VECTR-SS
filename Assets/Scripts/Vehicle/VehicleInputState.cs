using GTX.Core;
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
        public bool shiftDownHeld;

        public static VehicleInputState FromUnityInput()
        {
            float steerAxis = GTXInput.Axis("Horizontal");
            if (Input.GetKey(KeyCode.A))
            {
                steerAxis -= 1f;
            }

            if (Input.GetKey(KeyCode.D))
            {
                steerAxis += 1f;
            }

            float throttle = Input.GetKey(KeyCode.W) ? 1f : 0f;
            throttle = Mathf.Max(throttle, GTXInput.PositiveAxis("GTX_Throttle"));
            throttle = Mathf.Max(throttle, GTXInput.NegativeAxis("GTX_TriggerCombined"));
            throttle = Mathf.Max(throttle, GTXInput.Button(7) ? 1f : 0f);
            float brake = Input.GetKey(KeyCode.S) ? 1f : 0f;
            brake = Mathf.Max(brake, GTXInput.NegativeAxis("Vertical"));
            brake = Mathf.Max(brake, GTXInput.PositiveAxis("GTX_Brake"));
            brake = Mathf.Max(brake, GTXInput.PositiveAxis("GTX_TriggerCombined"));
            brake = Mathf.Max(brake, GTXInput.Button(6) ? 1f : 0f);
            float clutch = Input.GetKey(KeyCode.LeftShift) ? 1f : 0f;
            clutch = Mathf.Max(clutch, GTXInput.Button(4) ? 1f : 0f);

            VehicleInputState input = new VehicleInputState
            {
                steer = steerAxis,
                throttle = throttle,
                brake = brake,
                clutch = clutch,
                handbrake = Input.GetKey(KeyCode.Space) || GTXInput.Button(0),
                boost = Input.GetKey(KeyCode.F) || GTXInput.Button(1) || GTXInput.Button(5),
                shiftUp = Input.GetKeyDown(KeyCode.E) || GTXInput.ButtonDown(3),
                shiftDown = Input.GetKeyDown(KeyCode.Q) || GTXInput.ButtonDown(2),
                shiftDownHeld = Input.GetKey(KeyCode.Q) || GTXInput.Button(2)
            };

            input.steer = Mathf.Clamp(input.steer, -1f, 1f);
            input.throttle = Mathf.Clamp01(input.throttle);
            input.brake = Mathf.Clamp01(input.brake);
            input.clutch = Mathf.Clamp01(input.clutch);
            return input;
        }
    }
}
