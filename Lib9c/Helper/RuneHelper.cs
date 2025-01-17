using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Battle;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    public static class RuneHelper
    {
        public static Currency ToCurrency(
            RuneSheet.Row runeRow,
            byte decimalPlaces,
            [CanBeNull] IImmutableSet<Address> minters
        )
        {
            return new Currency(runeRow.Ticker, decimalPlaces: decimalPlaces, minters: minters);
        }

        public static FungibleAssetValue ToFungibleAssetValue(
            RuneSheet.Row runeRow,
            int quantity,
            byte decimalPlaces = 0,
            [CanBeNull] IImmutableSet<Address> minters = null
        )
        {
            return ToCurrency(runeRow, decimalPlaces, minters) * quantity;
        }

        public static List<FungibleAssetValue> CalculateReward(
            int rank,
            int bossId,
            RuneWeightSheet sheet,
            IWorldBossRewardSheet rewardSheet,
            RuneSheet runeSheet,
            IRandom random
        )
        {
            var row = sheet.Values.First(r => r.Rank == rank && r.BossId == bossId);
            var rewardRow = rewardSheet.OrderedRows.First(r => r.Rank == rank && r.BossId == bossId);
            if (rewardRow is WorldBossKillRewardSheet.Row kr)
            {
                kr.SetRune(random);
            }
            else if (rewardRow is WorldBossBattleRewardSheet.Row rr)
            {
                rr.SetRune(random);
            }
            var total = 0;
            var dictionary = new Dictionary<int, int>();
            while (total < rewardRow.Rune)
            {
                var selector = new WeightedSelector<int>(random);
                foreach (var info in row.RuneInfos)
                {
                    selector.Add(info.RuneId, info.Weight);
                }

                var ids = selector.Select(1);
                foreach (var id in ids)
                {
                    if (dictionary.ContainsKey(id))
                    {
                        dictionary[id] += 1;
                    }
                    else
                    {
                        dictionary[id] = 1;
                    }
                }

                total++;
            }

#pragma warning disable LAA1002
            var result = dictionary
#pragma warning restore LAA1002
                .Select(kv => ToFungibleAssetValue(runeSheet[kv.Key], kv.Value))
                .ToList();

            if (rewardRow.Crystal > 0)
            {
                result.Add(rewardRow.Crystal * CrystalCalculator.CRYSTAL);
            }
            return result;
        }
    }
}
