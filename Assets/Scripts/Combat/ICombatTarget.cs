using UnityEngine;

namespace GTX.Combat
{
    public interface ICombatTarget
    {
        Transform TargetTransform { get; }
        void ReceiveHit(CombatHit hit);
    }

    public struct CombatHit
    {
        public CombatHit(GameObject attacker, Vector3 point, Vector3 direction, float damage, float impulse, CombatHitType hitType)
        {
            Attacker = attacker;
            Point = point;
            Direction = direction;
            Damage = damage;
            Impulse = impulse;
            HitType = hitType;
        }

        public GameObject Attacker { get; }
        public Vector3 Point { get; }
        public Vector3 Direction { get; }
        public float Damage { get; }
        public float Impulse { get; }
        public CombatHitType HitType { get; }
    }

    public enum CombatHitType
    {
        SideSlam,
        BoostRam,
        SpinGuard
    }
}
