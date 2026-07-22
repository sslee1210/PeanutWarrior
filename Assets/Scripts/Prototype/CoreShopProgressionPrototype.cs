using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(7300)]
    public sealed class CoreShopProgressionPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private IdleSystemsPrototype pets;
        private PrototypeShopAndDaily legacyShop;
        private FieldInfo goldField;
        private FieldInfo diamondField;
        private FieldInfo eggsField;
        private FieldInfo incubatingField;
        private FieldInfo incubationRemainingField;
        private MethodInfo claimDailyMethod;
        private MethodInfo buyEggMethod;
        private string lastMessage = "상점 준비";

        public string LastMessage => lastMessage;
        public long Gold => goldField == null || arena == null ? 0L : Convert.ToInt64(goldField.GetValue(arena));
        public int Diamonds => diamondField == null || arena == null ? 0 : Convert.ToInt32(diamondField.GetValue(arena));
        public int Eggs => eggsField == null || pets == null ? 0 : Convert.ToInt32(eggsField.GetValue(pets));
        public bool IsIncubating => incubatingField != null && pets != null && Convert.ToBoolean(incubatingField.GetValue(pets));
        public float IncubationRemaining => incubationRemainingField == null || pets == null ? 0f : Convert.ToSingle(incubationRemainingField.GetValue(pets));
        public int GlobalStage => stageFlow == null ? 1 : PeanutGameRules.ToGlobalStage(stageFlow.World, stageFlow.Stage);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<CoreShopProgressionPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorCoreShopProgression");
            DontDestroyOnLoad(root);
            root.AddComponent<CoreShopProgressionPrototype>();
        }

        private void Start()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            pets = FindFirstObjectByType<IdleSystemsPrototype>();
            legacyShop = FindFirstObjectByType<PrototypeShopAndDaily>();
            if (arena == null || stageFlow == null)
            {
                enabled = false;
                return;
            }

            Type arenaType = typeof(CombatPrototypeArena);
            goldField = arenaType.GetField("gold", PrivateInstance);
            diamondField = arenaType.GetField("diamonds", PrivateInstance);
            if (pets != null)
            {
                Type petType = typeof(IdleSystemsPrototype);
                eggsField = petType.GetField("eggs", PrivateInstance);
                incubatingField = petType.GetField("incubating", PrivateInstance);
                incubationRemainingField = petType.GetField("incubationRemaining", PrivateInstance);
            }
            if (legacyShop != null)
            {
                Type shopType = typeof(PrototypeShopAndDaily);
                claimDailyMethod = shopType.GetMethod("ClaimDailyReward", PrivateInstance);
                buyEggMethod = shopType.GetMethod("BuyEgg", PrivateInstance);
            }
        }

        public bool ClaimDailyReward()
        {
            if (legacyShop == null || claimDailyMethod == null)
            {
                lastMessage = "접속 보상 시스템 연결 대기";
                return false;
            }
            claimDailyMethod.Invoke(legacyShop, null);
            lastMessage = legacyShop.ShopMessage;
            return !lastMessage.Contains("이미 수령");
        }

        public bool BuyPetEgg()
        {
            if (legacyShop == null || buyEggMethod == null)
            {
                lastMessage = "펫 알 상점 연결 대기";
                return false;
            }
            int before = Diamonds;
            buyEggMethod.Invoke(legacyShop, null);
            lastMessage = legacyShop.ShopMessage;
            return Diamonds < before;
        }

        public int GetGoldSupplyCost()
        {
            return 5;
        }

        public long GetGoldSupplyAmount()
        {
            return 500L + GlobalStage * 150L;
        }

        public bool BuyGoldSupply()
        {
            int cost = GetGoldSupplyCost();
            if (!SpendDiamonds(cost))
            {
                lastMessage = $"다이아 {cost}개 필요";
                return false;
            }
            long amount = GetGoldSupplyAmount();
            goldField.SetValue(arena, Gold + amount);
            lastMessage = $"성장 골드 보급 · {amount:N0}G 획득";
            return true;
        }

        public int GetIncubationFinishCost()
        {
            if (!IsIncubating) return 0;
            return Mathf.Clamp(Mathf.CeilToInt(IncubationRemaining / 30f), 1, 5);
        }

        public bool FinishIncubationNow()
        {
            if (!IsIncubating)
            {
                lastMessage = "진행 중인 부화가 없습니다";
                return false;
            }
            int cost = GetIncubationFinishCost();
            if (!SpendDiamonds(cost))
            {
                lastMessage = $"다이아 {cost}개 필요";
                return false;
            }
            incubationRemainingField.SetValue(pets, 0f);
            lastMessage = $"부화 즉시 완료 · 다이아 {cost}개 사용";
            return true;
        }

        private bool SpendDiamonds(int amount)
        {
            if (diamondField == null || amount < 0 || Diamonds < amount) return false;
            diamondField.SetValue(arena, Diamonds - amount);
            return true;
        }
    }
}
