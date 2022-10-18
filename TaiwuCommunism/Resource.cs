using Config;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Global;
using GameData.Domains.Item;
using GameData.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TaiwuCommunism
{
    public class Resource
    {
        // Is Taiwu included in resource sharing.
        public static bool includeTaiwu;

        // Whether cash is included in redistribution.
        public static bool includeCash;

        // When after redistribution there is still residue in terms of resources, whether to sell them to earn cash.
        public static bool sellOverflownResource;

        // 10 surplus resource = [exchangeRateCash] earned cash.
        public static int exchangeRateCash;

        // Instead of selling resources but give away a part of them to earn renown.
        public static bool giveAwayOverflownResource;

        // Percentage of giveaway in total overflown amount.
        public static int giveAwayRatio;

        // 10 surplus resource = [exchangeRateRenown] gained renown.
        public static int exchangeRateRenown;

        // Whether show a monthly notification "Earned cash/renown".
        public static bool sellOverflowNotification;

        // Taiwu's resources' lower bound (absolute value).
        // Taiwu joins redistribution only if their certain type of resource is greater than its corresponding value value. 
        public static int[] taiwuResourceLowerBound = new int[7];

        // Villager's resources' lower bound (absolute value).
        // A villager joins redistribution only if their certain type of resource is greater than its corresponding value value. 
        public static int[] villagerResourceLowerBound = new int[7];

        // Is end-month purchase allowed.
        // When true, the village will try purchase material items in nearby villages.
        public static bool purchaseRemainingCommodity;

        // Whether to purchase by types.
        public static bool buyFoodMaterial;
        public static bool buyWoodMaterial;
        public static bool buyOreMaterial;
        public static bool buyJadeMaterial;
        public static bool buyFabricMaterial;
        public static bool buyMedicineMaterial;
        public static bool buyPoisonMaterial;
        public static bool buyCraftTool;
        public static bool buyMedicine;
        public static bool buyPoison;
        public static bool buyWug;
        public static bool buyTea;
        public static bool buyWine;
        public static bool buyLifeSkillBook;
        public static bool buyBuildingCore;
        public static bool buyRope;

        // Max & Min grade for purchase
        public static int itemMaxGrade;
        public static int itemMinGrade;

        // Is area limitation of purchase unlocked.
        public static bool unlockAreaLimitation;

        // Whether show a monthly notification "Acquire item".
        public static bool buyItemNotification;

        // List of item subtypes.
        private const short FoodMaterial = 500;
        private const short WoodMaterial = 501;
        private const short OreMaterial = 502;
        private const short JadeMaterial = 503;
        private const short FabricMaterial = 504;
        private const short MedicineMaterial = 505;
        private const short PoisonMaterial = 506;
        private const short CraftTool = 600;
        private const short Medicine = 800;
        private const short Poison = 801;
        private const short Wug = 802;
        private const short Tea = 900;
        private const short Wine = 901;
        private const short LifeSkillBook = 1000;
        private const short BuildingCore = 1205;
        private const short Rope = 1206;

        private static bool isAllowedToPurchaseBySubtype(short itemSubtype){
            switch (itemSubtype) {
                case FoodMaterial:
                    return buyFoodMaterial;
                case WoodMaterial:
                    return buyWoodMaterial;
                case OreMaterial:
                    return buyOreMaterial;
                case JadeMaterial:
                    return buyJadeMaterial;
                case FabricMaterial:
                    return buyFabricMaterial;
                case MedicineMaterial:
                    return buyMedicineMaterial;
                case PoisonMaterial:
                    return buyPoisonMaterial;
                case CraftTool:
                    return buyCraftTool;
                case Medicine:
                    return buyMedicine;
                case Poison:
                    return buyPoison;
                case Wug:
                    return buyWug;
                case Tea:
                    return buyTea;
                case Wine:
                    return buyWine;
                case LifeSkillBook:
                    return buyLifeSkillBook;
                case BuildingCore:
                    return buyBuildingCore;
                case Rope:
                    return buyRope; 
                default:
                    return false;
            }
        }

        public static void Redistribute(DataContext context)
        {
            AdaptableLog.Info("开始重新分配");
            List<int> villagerIdsList = new List<int>();
            DomainManager.Organization.GetElement_CivilianSettlements(DomainManager.Taiwu.GetTaiwuVillageSettlementId()).GetMembers().GetAllMembers(villagerIdsList);
            GameData.Domains.Character.Character distributor;
            int distributorId = DomainManager.Taiwu.GetTaiwuCharId();
            if (!includeTaiwu)
            {
                villagerIdsList.Remove(DomainManager.Taiwu.GetTaiwuCharId());

                Random random = new Random();
                bool isDistributorFound;
                int counter = 0;
                do
                {
                    distributorId = villagerIdsList[random.Next(0, villagerIdsList.Count)];
                    isDistributorFound = DomainManager.Character.TryGetElement_Objects(distributorId, out distributor);
                    counter++;
                } while (!isDistributorFound && counter < 3);
                if (!isDistributorFound)
                {
                    AdaptableLog.Warning("未找到分配主持人");
                    return;
                }
            }
            else
            {
                bool isTaiwuFound = DomainManager.Character.TryGetElement_Objects(distributorId, out distributor);
                if (!isTaiwuFound)
                {
                    AdaptableLog.Warning("未找到太吾");
                    return;
                }
            }
            sbyte resourceTypesCount = includeCash ? (sbyte)7 : (sbyte)6;
            int actualVillagerCount = 1;
            
            foreach (int id in villagerIdsList)
            {
                if (id == distributorId)
                {
                    continue;
                }
                GameData.Domains.Character.Character villager;
                bool exists = DomainManager.Character.TryGetElement_Objects(id, out villager);
                if (!exists)
                {
                    continue;
                }
                // AdaptableLog.Info(String.Format("村民{0}{1}上缴资源", villager.GetSurname(), villager.GetGivenName()));
                actualVillagerCount++;
                for (sbyte i = 0; i < resourceTypesCount; i++)
                {
                    int resource = villager.GetResource(i);
                    // AdaptableLog.Info(String.Format("村民{0}{1}拥有{2}的资源{3}", villager.GetSurname(), villager.GetGivenName(), resource.ToString(), i.ToString()));
                    if ( resource < villagerResourceLowerBound[i] ) { continue; }
                    int surplus = resource - villagerResourceLowerBound[i];
                    villager.ChangeResource(context, i, -surplus);
                    distributor.ChangeResource(context, i, surplus);
                    // AdaptableLog.Info(String.Format("村民{0}上缴了{1}{2}的资源{3}", villager.GetSurname(), villager.GetGivenName(), surplus.ToString(), i.ToString()));
                }
            }

            if (purchaseRemainingCommodity) {
                PurchaseNearbyMerchantCommodity(context, distributorId);
            }

            int upperLimit = DomainManager.Taiwu.GetMaterialResourceMaxCount();
            for (sbyte i = 0; i< resourceTypesCount; i++)
            {
                int resource = distributor.GetResource(i);
                int lowerBound = includeTaiwu ? taiwuResourceLowerBound[i] : villagerResourceLowerBound[i];
                if (resource <= lowerBound) { continue; }
                int surplus = resource - lowerBound;
                int acquire = surplus / actualVillagerCount;
                if (i != 6 && lowerBound < upperLimit) {
                    acquire = Math.Min(acquire, upperLimit - lowerBound);
                }
                foreach (int id in villagerIdsList)
                {
                    if (id == distributorId)
                    {
                        continue;
                    }
                    GameData.Domains.Character.Character villager;
                    bool exists = DomainManager.Character.TryGetElement_Objects(id, out villager);
                    if (!exists)
                    {
                        continue;
                    }
                    villager.ChangeResource(context, i, acquire);
                    distributor.ChangeResource(context, i, -acquire);
                    // AdaptableLog.Info(String.Format("村民{0}{1}得到了{2}的资源{3}", villager.GetSurname(), villager.GetGivenName(), acquire.ToString(), i.ToString()));
                }
            }

            if (sellOverflownResource)
            {
                SellTaiwuOverflownResource(context);
            }
        }

        public static void PurchaseNearbyMerchantCommodity(DataContext context) {
            PurchaseNearbyMerchantCommodity(context, DomainManager.Taiwu.GetTaiwuCharId());
        }

        private static void PurchaseNearbyMerchantCommodity(DataContext context, int traderId)
        {
            AdaptableLog.Info("开始进货");
            short taiwuVillageAreaId = DomainManager.Organization.GetElement_CivilianSettlements(DomainManager.Taiwu.GetTaiwuVillageSettlementId()).GetLocation().AreaId;
            List<GameData.Domains.Organization.Settlement> settlements = new List<GameData.Domains.Organization.Settlement>();
            DomainManager.Organization.GetAllCivilianSettlements(settlements);
            GameData.Domains.Character.Character trader;
            bool traderExists = DomainManager.Character.TryGetElement_Objects(traderId, out trader);
            if (!traderExists)
            {
                return;
            }
            int cash = trader.GetResource(6) - (traderId == DomainManager.Taiwu.GetTaiwuCharId() ? taiwuResourceLowerBound[6] : villagerResourceLowerBound[6]);
            if (cash <= 0) { return; }
            // AdaptableLog.Info(String.Format("持有银钱{0}", cash));
            trader.ChangeResource(context, 6, -cash);

            foreach (GameData.Domains.Organization.Settlement settlement in settlements) {
                // AdaptableLog.Info(String.Format("开始探查据点{0}, 位于区域{1}", settlement.GetId(), settlement.GetLocation().AreaId));
                if (!unlockAreaLimitation && taiwuVillageAreaId != settlement.GetLocation().AreaId) {
                    // AdaptableLog.Info("不在太吾村区域");
                    continue; 
                }
                List<int> civillianIdList = new List<int>();
                settlement.GetMembers().GetAllMembers(civillianIdList);
                foreach (int id in civillianIdList)
                {
                    GameData.Domains.Character.Character civillian;
                    bool exists = DomainManager.Character.TryGetElement_Objects(id, out civillian);
                    if (!exists)
                    {
                        continue;
                    }
                    // AdaptableLog.Info(String.Format("尝试和{0}开始贸易", civillian.GetId()));
                    GameData.Domains.Merchant.MerchantData merchantData;
                    if (!DomainManager.Merchant.TryGetMerchantData(id, out merchantData)) {
                        // AdaptableLog.Info("不是商人");
                        continue;
                    }
                    sbyte merchantType;
                    if (!DomainManager.Extra.TryGetMerchantCharToType(id, out merchantType))
                    {
                        // AdaptableLog.Info("商人种类无法获取");
                        continue;
                    }
                    // AdaptableLog.Info(String.Format("商人种类{0}", merchantType));
                    if (!unlockAreaLimitation && civillian.GetLocation().AreaId != taiwuVillageAreaId)
                    {
                        // AdaptableLog.Info("商人外出");
                        continue;
                    }
                    // int merchantFavorability = DomainManager.Merchant.GetMerchantFavorability()[merchantType];
                    // TODO(ameki): 考虑乱序避免总是用垃圾填满？
                    for (int i = 0; i < 7 ; ++i)
                    {
                        List<ItemKey> itemkeys = new List<ItemKey>();
                        Dictionary<ItemKey, int> items = merchantData.GetGoodsList(i).Items;
                        foreach(KeyValuePair<ItemKey, int> item in items) {
                            short itemSubtype = ItemTemplateHelper.GetItemSubType(item.Key.ItemType, item.Key.TemplateId);
                            short itemGrade = ItemTemplateHelper.GetGrade(item.Key.ItemType, item.Key.TemplateId);
                            // AdaptableLog.Info(String.Format("商人有{1}个{0}，品级为{2}。", ItemTemplateHelper.GetName(item.Key.ItemType, item.Key.TemplateId), item.Value, itemGrade));
                            if (!isAllowedToPurchaseBySubtype(itemSubtype)) { continue; }
                            if (itemGrade < itemMaxGrade) { continue; }
                            if (itemGrade > itemMinGrade) { continue; }
                            itemkeys.Add(item.Key);
                        }

                        itemkeys = itemkeys.OrderBy(a => Guid.NewGuid()).ToList();
                        foreach (ItemKey key in itemkeys) {
                            int count = items[key];
                            short itemSubtype = ItemTemplateHelper.GetItemSubType(key.ItemType, key.TemplateId);
                            int price = ItemTemplateHelper.GetBasePrice(key.ItemType, key.TemplateId);
                            int weight = DomainManager.Item.GetBaseItem(key).GetWeight();
                            int headroom = DomainManager.Taiwu.GetWarehouseMaxLoad() - DomainManager.Taiwu.GetWarehouseCurrLoad();
                            int maxCanBuy = count;
                            if (weight != 0)
                            {
                                maxCanBuy = Math.Min(maxCanBuy, headroom / weight);
                            }
                            if (price != 0)
                            {
                                maxCanBuy = Math.Min(maxCanBuy, cash / price);
                            }
                            if (maxCanBuy <= 0) { continue; }

                            DomainManager.Taiwu.WarehouseAdd(context, key, maxCanBuy);
                            DomainManager.Merchant.RemoveExistingMerchantItem(context, civillian.GetId(), key, maxCanBuy);
                            DomainManager.Merchant.ChangeMerchantCumulativeMoney(context, merchantType, price * maxCanBuy);
                            cash -= maxCanBuy * price;

                            if (buyItemNotification) {
                                for (int j = 0; j < maxCanBuy; ++j) {
                                    DomainManager.World.GetInstantNotificationCollection().AddGetItem(DomainManager.Taiwu.GetTaiwuCharId(), key.ItemType, key.TemplateId);
                                }
                            }
                        }
                    }
                }
            }
            trader.ChangeResource(context, 6, cash);
        }

        public static void SellTaiwuOverflownResource(DataContext context)
        {
            AdaptableLog.Info("开始溢出资源处理");
            GameData.Domains.Character.Character taiwu;
            bool exists = DomainManager.Character.TryGetElement_Objects(DomainManager.Taiwu.GetTaiwuCharId(), out taiwu);
            if (!exists)
            {
                return;
            }
            int upperLimit = DomainManager.Taiwu.GetMaterialResourceMaxCount();
            int totalRenownGained = 0;
            int totalCashEarned = 0;
            for (sbyte i = 0; i < 6; i++) {
                int resource = taiwu.GetResource(i);
                if (resource <= upperLimit) { continue; }
                int surplus = resource - upperLimit;
                taiwu.ChangeResource(context, i, -surplus);
                int maybeGiveAwaySurplus = surplus * giveAwayRatio / 100;
                surplus -= maybeGiveAwaySurplus;
                if (giveAwayOverflownResource) {
                    int renownGained = maybeGiveAwaySurplus * exchangeRateRenown / 10;
                    totalRenownGained += renownGained;
                    taiwu.ChangeResource(context, 7, renownGained);
                    // AdaptableLog.Info(String.Format("太吾出售{0}资源{1}换取{2}声望", maybeGiveAwaySurplus, i, renownGained));
                }
                else {
                    int cashGained = maybeGiveAwaySurplus * exchangeRateCash / 10;
                    totalCashEarned += cashGained;
                    taiwu.ChangeResource(context, 6, cashGained);
                    // AdaptableLog.Info(String.Format("太吾出售{0}资源{1}换取{2}银钱", maybeGiveAwaySurplus, i, cashGained));
                }
                int cashEarned = surplus * exchangeRateCash / 10;
                totalCashEarned += cashEarned;
                taiwu.ChangeResource(context, 6, cashEarned);
                // AdaptableLog.Info(String.Format("太吾出售{0}资源{1}换取{2}银钱", surplus, i, cashEarned));
            }
            if (sellOverflowNotification) {
                DomainManager.World.GetInstantNotificationCollection().AddResourceIncreased(DomainManager.Taiwu.GetTaiwuCharId(), 6, totalCashEarned);
                DomainManager.World.GetInstantNotificationCollection().AddResourceIncreased(DomainManager.Taiwu.GetTaiwuCharId(), 7, totalRenownGained);
            }
            AdaptableLog.Info(String.Format("太吾共获得{0}银钱{1}声望", totalCashEarned, totalRenownGained));
        }
    }
}
