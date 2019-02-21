using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupremePlayServer
{
    class systemdata
    {
        public List<String> getAllpacketList()
        {
            List<String> plist = new List<string>();

            plist.Add("<chat1>");
            plist.Add("<bigsay>");
            plist.Add("<23>");
            plist.Add("<27>");
            plist.Add("<partyhill>");
            plist.Add("<Drop>");
            plist.Add("<drop_create>");
            plist.Add("<drop_del>");
            plist.Add("<Guild_Message>");
            plist.Add("<party>");
            plist.Add("<summon>");
            plist.Add("<all_summon>");
            plist.Add("<prison>");
            plist.Add("<partymessage>");
            plist.Add("<whispers>");
            plist.Add("<party_no>");
            plist.Add("<System_Message>");
            plist.Add("<event_animation>");
            plist.Add("<guild_group>");
            plist.Add("<guild_invite>");
            plist.Add("<guild_delete>");
            plist.Add("<guild_message>");
            plist.Add("<player_animation>");
            plist.Add("<trade_invite>");
            plist.Add("<trade_system>");
            plist.Add("<trade_item>");
            plist.Add("<trade_money>");
            plist.Add("<trade_okay>");
            plist.Add("<trade_fail>");
            plist.Add("<nptreq>");
            plist.Add("<nptreq2>");
            plist.Add("<nptreq3>");
            plist.Add("<nptno>");
            plist.Add("<nptyes>");
            plist.Add("<nptyes1>");
            plist.Add("<nptyes2>");
            plist.Add("<nptout>");
            plist.Add("<nptgain>");
            plist.Add("<cashgive>");

            return plist;
        }
    }
}
