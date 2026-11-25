using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatTanks
{
    public sealed partial class CatTanksGame
    {
        const string SummonTabLabel = "Gọi đồng đội";
        // Nhận rương AFK: dùng chung cho trang Rương AFK và ô rương trên màn chính.
        void ClaimIdle()
        {
            var rewards=profile.Idle(Profile.Now);profile.Grant(rewards);profile.lastIdle=Math.Max(profile.lastIdle,Profile.Now);profile.Track("idle");Changed("Đã nhận chiến lợi phẩm AFK");
        }
    }
}
