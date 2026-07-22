using PeanutWarrior.Core;
using PeanutWarrior.Data;
using UnityEngine;

namespace PeanutWarrior.Combat
{
    public class PlayerController : MonoBehaviour
    {
        public float CurrentHp { get; private set; }
        public float CurrentMp { get; private set; }
        public bool IsDead => CurrentHp <= 0f;

        private PlayerStats Stats => GameManager.Instance.State.playerStats;

        private void Start() => FullRestore();

        private void Update()
        {
            if (IsDead) return;
            CurrentHp = Mathf.Min(Stats.MaxHp, CurrentHp + Stats.HpRegen * Time.deltaTime);
            CurrentMp = Mathf.Min(Stats.MaxMp, CurrentMp + Stats.MpRegen * Time.deltaTime);
        }

        public void TakeDamage(float damage)
        {
            if (damage <= 0f || IsDead) return;
            CurrentHp = Mathf.Max(0f, CurrentHp - damage);
            if (IsDead) Stage.StageManager.Instance.HandlePlayerDeath();
        }

        public bool TrySpendMp(float amount)
        {
            if (amount < 0f || CurrentMp < amount) return false;
            CurrentMp -= amount;
            return true;
        }

        public void FullRestore()
        {
            CurrentHp = Stats.MaxHp;
            CurrentMp = Stats.MaxMp;
        }
    }
}
