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
    [PluginConfig("TaiwuCommunism", "AmekiKyou", "1.2")]
    public class TaiwuCommunism : TaiwuRemakePlugin
    {
        private Harmony harmony;

        // Is resource redistribution allowed.
        // When true, villagers turns in resources at end of the month, and distributes equally.
        private static bool resourceRedistribution;

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

            DomainManager.Mod.GetSetting(base.ModIdStr, "PurchaseRemainingCommodity", ref Resource.purchaseRemainingCommodity);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyFoodMaterial", ref Resource.buyFoodMaterial);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyWoodMaterial", ref Resource.buyWoodMaterial);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyOreMaterial", ref Resource.buyOreMaterial);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyJadeMaterial", ref Resource.buyJadeMaterial);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyFabricMaterial", ref Resource.buyFabricMaterial);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyMedicineMaterial", ref Resource.buyMedicineMaterial);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyCraftTool", ref Resource.buyCraftTool);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyMedicine", ref Resource.buyMedicine);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyPoison", ref Resource.buyPoison);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyWug", ref Resource.buyWug);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyTea", ref Resource.buyTea);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyWine", ref Resource.buyWine);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyLifeSkillBook", ref Resource.buyLifeSkillBook);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyRope", ref Resource.buyRope);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyBuildingCore", ref Resource.buyBuildingCore);
            DomainManager.Mod.GetSetting(base.ModIdStr, "ItemMaxGrade", ref Resource.itemMaxGrade);
            DomainManager.Mod.GetSetting(base.ModIdStr, "ItemMinGrade", ref Resource.itemMinGrade);
            DomainManager.Mod.GetSetting(base.ModIdStr, "UnlockAreaLimitation", ref Resource.unlockAreaLimitation);
            DomainManager.Mod.GetSetting(base.ModIdStr, "BuyItemNotification", ref Resource.unlockAreaLimitation);
            DomainManager.Mod.GetSetting(base.ModIdStr, "ReservedWarehouseHeadroom", ref Resource.reservedWarehouseHeadroom);
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
            } else if (Resource.sellOverflownResource)
            {
                if (Resource.purchaseRemainingCommodity) { Resource.PurchaseNearbyMerchantCommodity(context); }
                Resource.SellTaiwuOverflownResource(context);
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
