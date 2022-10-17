using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaiwuModdingLib;
using TaiwuModdingLib.Core.Plugin;
using HarmonyLib;
using GameData.Domains;
using GameData.Domains.World;
using GameData.Common;

namespace TaiwuCommunism
{
    [PluginConfig("TaiwuCommunism", "AmekiKyou", "1.0")]
    public class TaiwuCommunism : TaiwuRemakePlugin
    {
        private Harmony harmony;

        // Is resource redistribution allowed.
        // When true, villagers turns in resources at end of the month, and distributes equally.
        private static bool resourceRedistribution;

        // TODO(ameki): implement purchase item logic
        // Is end-month purchase allowed.
        // When true, the village will try purchase material items in nearby villages.
        private static bool purchaseRemainingCommodity;

        public override void OnModSettingUpdate()
        {
            DomainManager.Mod.GetSetting(base.ModIdStr, "ResourceRedistribution", ref TaiwuCommunism.resourceRedistribution);
            DomainManager.Mod.GetSetting(base.ModIdStr, "IncludeTaiwu", ref Resource.includeTaiwu);
            DomainManager.Mod.GetSetting(base.ModIdStr, "IncludeCash", ref Resource.includeCash);
            DomainManager.Mod.GetSetting(base.ModIdStr, "SellOverflownResource", ref Resource.sellOverflownResource);
            DomainManager.Mod.GetSetting(base.ModIdStr, "ExchangeRateCash", ref Resource.exchangeRateCash);
            DomainManager.Mod.GetSetting(base.ModIdStr, "GiveAwayOverflownResource", ref Resource.giveAwayOverflownResource);
            DomainManager.Mod.GetSetting(base.ModIdStr, "GiveAwayRatio", ref Resource.giveAwayRatio);
            DomainManager.Mod.GetSetting(base.ModIdStr, "ExchangeRateRenown", ref Resource.exchangeRateRenown);
            DomainManager.Mod.GetSetting(base.ModIdStr, "SellOverflowNotification", ref Resource.sellOverflowNotification);

            DomainManager.Mod.GetSetting(base.ModIdStr, "TaiwuResourceLowerBoundFood", ref Resource.taiwuResourceLowerBound[0]);
            DomainManager.Mod.GetSetting(base.ModIdStr, "TaiwuResourceLowerBoundWood", ref Resource.taiwuResourceLowerBound[1]);
            DomainManager.Mod.GetSetting(base.ModIdStr, "TaiwuResourceLowerBoundOre", ref Resource.taiwuResourceLowerBound[2]);
            DomainManager.Mod.GetSetting(base.ModIdStr, "TaiwuResourceLowerBoundJade", ref Resource.taiwuResourceLowerBound[3]);
            DomainManager.Mod.GetSetting(base.ModIdStr, "TaiwuResourceLowerBoundFabric", ref Resource.taiwuResourceLowerBound[4]);
            DomainManager.Mod.GetSetting(base.ModIdStr, "TaiwuResourceLowerBoundMedicine", ref Resource.taiwuResourceLowerBound[5]);
            DomainManager.Mod.GetSetting(base.ModIdStr, "TaiwuResourceLowerBoundCash", ref Resource.taiwuResourceLowerBound[6]);

            DomainManager.Mod.GetSetting(base.ModIdStr, "VillagerResourceLowerBoundFood", ref Resource.villagerResourceLowerBound[0]);
            DomainManager.Mod.GetSetting(base.ModIdStr, "VillagerResourceLowerBoundWood", ref Resource.villagerResourceLowerBound[1]);
            DomainManager.Mod.GetSetting(base.ModIdStr, "VillagerResourceLowerBoundOre", ref Resource.villagerResourceLowerBound[2]);
            DomainManager.Mod.GetSetting(base.ModIdStr, "VillagerResourceLowerBoundJade", ref Resource.villagerResourceLowerBound[3]);
            DomainManager.Mod.GetSetting(base.ModIdStr, "VillagerResourceLowerBoundFabric", ref Resource.villagerResourceLowerBound[4]);
            DomainManager.Mod.GetSetting(base.ModIdStr, "VillagerResourceLowerBoundMedicine", ref Resource.villagerResourceLowerBound[5]);
            DomainManager.Mod.GetSetting(base.ModIdStr, "VillagerResourceLowerBoundCash", ref Resource.villagerResourceLowerBound[6]);

            DomainManager.Mod.GetSetting(base.ModIdStr, "PurchaseRemainingCommodity", ref TaiwuCommunism.purchaseRemainingCommodity);
        }

        public override void Initialize() {
            harmony = Harmony.CreateAndPatchAll(typeof(TaiwuCommunism), null);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldDomain), "AdvanceMonth")]
        public static void WorldDomain_AdvanceMonth_Prefix(DataContext context)
        {
            if (resourceRedistribution)
            {
                Resource.Redistribute(context);
            }
        }

        public override void Dispose()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
        }
    }
}
