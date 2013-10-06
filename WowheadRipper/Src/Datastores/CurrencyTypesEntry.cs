using System;
using System.Linq;
using System.Text;
using DBFilesClient.NET;

namespace WowheadRipper
{
    enum CurrencyFlags
    {
        CURRENCY_FLAG_TRADEABLE          = 0x01,
        CURRENCY_FLAG_HIGH_PRECISION     = 0x08,
        CURRENCY_FLAG_COUNT_SEASON_TOTAL = 0x80,
    };

    public class CurrencyTypesEntry
    {
        public UInt32 ID;
        public UInt32 Category;
        public String Name;
        public String IconName;
        public UInt32 Unk4;
        public UInt32 HasSubstitution;
        public UInt32 SubstitutionId;
        public UInt32 TotalCap;
        public UInt32 WeekCap;
        public UInt32 Flags;
        public String Description;
    }
}
