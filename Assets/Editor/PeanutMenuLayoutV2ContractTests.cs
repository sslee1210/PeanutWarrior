#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using PeanutWarrior.Prototype;
using UnityEngine;

namespace PeanutWarrior.Tests
{
    public sealed class PeanutMenuLayoutV2ContractTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void LayoutV2_ExposesFinalMenuContract()
        {
            Assert.IsTrue(typeof(PeanutMenuLayoutV2).IsSubclassOf(typeof(MonoBehaviour)));
            Assert.NotNull(typeof(PeanutMenuLayoutV2).GetProperty("LayoutVersion", PublicInstance));
            Assert.NotNull(typeof(PeanutMenuLayoutV2).GetProperty("ManagedPageCount", PublicInstance));
            Assert.NotNull(typeof(PeanutMenuLayoutV2).GetProperty("UsesTwoColumnGrowth", PublicInstance));
            Assert.NotNull(typeof(PeanutMenuLayoutV2).GetProperty("UsesConstantButtonBackgrounds", PublicInstance));
        }

        [Test]
        public void LayoutV2_ContainsAllEightInnerPageBuilders()
        {
            string[] builders =
            {
                "BuildSkills", "BuildEquipment", "BuildGrowth", "BuildAdvancement",
                "BuildPets", "BuildShop", "BuildStageSelect", "BuildSettings"
            };

            foreach (string builder in builders)
                Assert.NotNull(typeof(PeanutMenuLayoutV2).GetMethod(builder, PrivateInstance), builder);
        }

        [Test]
        public void LayoutV2_KeepsGlobalAutoAndFinalGrowthActions()
        {
            Assert.NotNull(typeof(PeanutMenuLayoutV2).GetMethod("ToggleGlobalAuto", PrivateInstance));
            Assert.NotNull(typeof(PeanutMenuLayoutV2).GetMethod("UpgradeStat", PrivateInstance));
            Assert.NotNull(typeof(PeanutMenuLayoutV2).GetMethod("EquipElement", PrivateInstance));
            Assert.NotNull(typeof(PeanutMenuLayoutV2).GetMethod("SelectStage", PrivateInstance));
        }
    }
}
#endif
