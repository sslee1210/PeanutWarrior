using UnityEngine;

namespace PeanutWarrior.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; } = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AddGold(long amount) => State.gold += System.Math.Max(0, amount);
        public void AddSkillFragments(long amount) => State.skillFragments += System.Math.Max(0, amount);
        public void AddDiamonds(long amount) => State.diamonds += System.Math.Max(0, amount);
    }
}
