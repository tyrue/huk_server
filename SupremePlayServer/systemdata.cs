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

            plist.Add("<chat>");    // 공지
            plist.Add("<chat1>");   // 일반 채팅
            plist.Add("<bigsay>");  // 외치기
            plist.Add("<23>");      // 몹 정보 공유
            plist.Add("<27>");      // 
            plist.Add("<partyhill>"); // 파티 힐
            plist.Add("<Drop>");    // 버리기
            plist.Add("<drop_create>"); // 템 드랍
            plist.Add("<drop_del>");    // 템 삭제
            plist.Add("<Guild_Message>"); // 길드 메시지
            plist.Add("<party>");   // 파티
            plist.Add("<summon>");
            plist.Add("<all_summon>");
            plist.Add("<prison>");  // 감옥
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
            plist.Add("<npt_move>"); // 파티 장소 이동
            plist.Add("<cashgive>");
            plist.Add("<switches>"); // 스위치 공유
            plist.Add("<variables>"); // 변수 공유
            plist.Add("<enemy_dead>"); // 몹 죽음 공유
            plist.Add("<respawn>"); // 몹 부활 공유
            plist.Add("<8>"); // 유저 죽음 알림
            plist.Add("<hp>"); // 몹 체력 공유
            plist.Add("<mon_move>"); // 몬스터 이동 공유
            plist.Add("<aggro>"); // 몬스터 어그로
            plist.Add("<mon_damage>"); // 몬스터 데미지 표시
            plist.Add("<player_damage>"); // 플레이어 데미지 표시
            return plist;
        }
    }
}
