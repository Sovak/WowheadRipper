using System;
using System.Linq;
using System.Text;
using DBFilesClient.NET;

namespace WowheadRipper
{
    public class ItemExtendedCostEntry
    {
        public UInt32   ID;
        public UInt32   ReqiredHonorPoints;
        public UInt32   ReqiredArenaPoints;
        public UInt32   RequiredArenaSlot;
        [StoragePresence(StoragePresenceOption.Include, ArraySize = 5)]
        public UInt32[] RequiredItem;
        [StoragePresence(StoragePresenceOption.Include, ArraySize = 5)]
        public UInt32[] RequiredItemCount;
        public UInt32   RequiredPersonalArenaRating;
        public UInt32   ItemPurchaseGroup;
        [StoragePresence(StoragePresenceOption.Include, ArraySize = 5)]
        public UInt32[] RequiredCurrency;
        [StoragePresence(StoragePresenceOption.Include, ArraySize = 5)]
        public UInt32[] RequiredCurrencyCount;
        [StoragePresence(StoragePresenceOption.Include, ArraySize = 5)]
        public UInt32[] Unknown;
    }
}
