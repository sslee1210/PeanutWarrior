using UnityEngine;

namespace PeanutWarrior.Prototype
{
    public sealed class PrototypeEnemy : MonoBehaviour
    {
        public bool IsBoss { get; private set; }
        public float CurrentHp { get; private set; }
        public float Attack { get; private set; }

        private PrototypeGame game;
        private float attackTimer;

        public void Initialize(PrototypeGame owner, bool isBoss, float hp, float attack)
        {
            game = owner;
            IsBoss = isBoss;
            CurrentHp = hp;
            Attack = attack;
            transform.localScale = isBoss ? Vector3.one * 1.8f : Vector3.one;
            GetComponent<SpriteRenderer>().color = isBoss
                ? new Color(0.55f, 0.12f, 0.12f)
                : new Color(0.25f, 0.65f, 0.25f);
        }

        private void Update()
        {
            if (game == null || game.IsPlayerDead) return;
            attackTimer -= Time.deltaTime;
            if (attackTimer > 0f) return;
            attackTimer = IsBoss ? 0.8f : 1.4f;
            game.DamagePlayer(Attack);
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || CurrentHp <= 0f) return;
            CurrentHp -= amount;
            if (CurrentHp > 0f) return;
            game.OnEnemyDefeated(this);
            Destroy(gameObject);
        }
    }
}
