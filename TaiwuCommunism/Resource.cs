using GameData.Common;
using GameData.Domains;
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

        // Whether show a monthly notification "Unexpectly earned cash/renown".
        public static bool sellOverflowNotification;

        // Taiwu's resources' lower bound (absolute value).
        // Taiwu joins redistribution only if their certain type of resource is greater than its corresponding value value. 
        public static int[] taiwuResourceLowerBound = new int[7];

        // Villager's resources' lower bound (absolute value).
        // A villager joins redistribution only if their certain type of resource is greater than its corresponding value value. 
        public static int[] villagerResourceLowerBound = new int[7];

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
                    AdaptableLog.Info(String.Format("村民{0}{1}得到了{2}的资源{3}", villager.GetSurname(), villager.GetGivenName(), acquire.ToString(), i.ToString()));
                }
            }

            if (sellOverflownResource)
            {
                SellTaiwuOverflownResource(context);
            }
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
