using GTX.Data;
using UnityEngine;

namespace GTX.Vehicle
{
    public enum ShiftQuality
    {
        None,
        Perfect,
        Bad
    }

    [System.Serializable]
    public sealed class GearboxController
    {
        public const int ReverseGear = -1;
        public const int NeutralGear = 0;
        public const int MinForwardGear = 1;
        public const int MaxForwardGear = 6;

        public int CurrentGear { get; private set; } = 1;
        public bool IsShifting { get; private set; }
        public ShiftQuality LastShiftQuality { get; private set; }
        public float LastShiftAge { get; private set; } = 99f;
        public float ShiftProgress => shiftDuration <= 0f ? 1f : Mathf.Clamp01(shiftTimer / shiftDuration);
        public float CurrentRatio => currentRatio;

        private float currentRatio = 1f;
        private float shiftTimer;
        private float shiftCooldownTimer;
        private float shiftDuration;

        public void Reset(VehicleTuning tuning)
        {
            CurrentGear = 1;
            currentRatio = GetRatioForGear(tuning, CurrentGear);
            IsShifting = false;
            LastShiftQuality = ShiftQuality.None;
            LastShiftAge = 99f;
            shiftTimer = 0f;
            shiftCooldownTimer = 0f;
            shiftDuration = tuning != null ? tuning.shiftDuration : 0.18f;
        }

        public void Tick(VehicleTuning tuning, VehicleInputState input, float engineRpm, float deltaTime)
        {
            LastShiftAge += deltaTime;
            shiftCooldownTimer = Mathf.Max(0f, shiftCooldownTimer - deltaTime);

            if (IsShifting)
            {
                shiftTimer += deltaTime;
                if (shiftTimer >= shiftDuration)
                {
                    IsShifting = false;
                    currentRatio = GetRatioForGear(tuning, CurrentGear);
                }
            }

            if (input.shiftUp)
            {
                RequestShift(tuning, CurrentGear + 1, engineRpm);
            }
            else if (input.shiftDown)
            {
                RequestShift(tuning, CurrentGear - 1, engineRpm);
            }
        }

        public bool RequestShift(VehicleTuning tuning, int targetGear, float engineRpm)
        {
            if (tuning == null || IsShifting || shiftCooldownTimer > 0f)
            {
                return false;
            }

            targetGear = Mathf.Clamp(targetGear, ReverseGear, MaxForwardGear);
            if (targetGear == CurrentGear)
            {
                return false;
            }

            CurrentGear = targetGear;
            shiftDuration = Mathf.Max(0.01f, tuning.shiftDuration);
            shiftTimer = 0f;
            shiftCooldownTimer = tuning.shiftCooldown + shiftDuration;
            IsShifting = true;
            LastShiftAge = 0f;
            LastShiftQuality = JudgeShift(tuning, engineRpm);
            currentRatio = 0f;
            return true;
        }

        public float GetShiftTorqueMultiplier(VehicleTuning tuning)
        {
            if (tuning == null || LastShiftAge > tuning.shiftJudgementWindow)
            {
                return 1f;
            }

            if (LastShiftQuality == ShiftQuality.Perfect)
            {
                return tuning.perfectShiftTorqueMultiplier;
            }

            return LastShiftQuality == ShiftQuality.Bad ? tuning.badShiftTorqueMultiplier : 1f;
        }

        public float GetRatioForGear(VehicleTuning tuning, int gear)
        {
            if (tuning == null || gear == NeutralGear)
            {
                return 0f;
            }

            if (gear == ReverseGear)
            {
                return tuning.reverseRatio;
            }

            if (tuning.forwardRatios == null || tuning.forwardRatios.Length == 0)
            {
                return 0f;
            }

            int ratioIndex = Mathf.Clamp(gear - 1, 0, tuning.forwardRatios.Length - 1);
            return tuning.forwardRatios[ratioIndex];
        }

        private ShiftQuality JudgeShift(VehicleTuning tuning, float engineRpm)
        {
            if (CurrentGear <= NeutralGear)
            {
                return ShiftQuality.None;
            }

            if (engineRpm >= tuning.perfectShiftRpmMin && engineRpm <= tuning.perfectShiftRpmMax)
            {
                return ShiftQuality.Perfect;
            }

            return ShiftQuality.Bad;
        }
    }
}
