using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SupremePlayServer
{
    public class systemdata
    {
        public Dictionary<int, List<Monster>> monster_data; // 맵에 존재하는 몬스터의 데이터를 저장
        public Dictionary<int, List<Item>> item_data; // 맵에 존재하는 아이템의 데이터를 저장
        public Dictionary<int, string> map_data; // 맵의 이름 저장
        System_DB system_db;
        
        public systemdata()
        {
            monster_data = new Dictionary<int, List<Monster>>();
            item_data = new Dictionary<int, List<Item>>();
            map_data = new Dictionary<int, string>();
            system_db = new System_DB();
            map_data = system_db.SendMap();
        }

        public List<String> getAllpacketList()
        {
            List<String> plist = new List<string>();

            plist.Add("<chat>");    // 공지
            plist.Add("<chat1>");   // 일반 채팅
            plist.Add("<bigsay>");  // 외치기
            plist.Add("<23>");      // 몹 정보 공유
            plist.Add("<27>");      // 애니메이션 공유
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
            plist.Add("<nptno>");
            plist.Add("<nptyes>");
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
            plist.Add("<map_chat>"); // 플레이어 말풍선 표시
            plist.Add("<attack_effect>"); // pvp 평타
            plist.Add("<skill_effect>"); // pvp 스킬
            return plist;
        }

        public void SaveMonster(string data)
        {
            string[] d = data.Split(',');
            bool sw = false;
            if (!monster_data.ContainsKey(int.Parse(d[0])))
            {
                monster_data[int.Parse(d[0])] = new List<Monster>();
                sw = false;
            }
            else
            {
                Monster ii = null;
                foreach (var m in monster_data[int.Parse(d[0])])
                {
                    if(m.map_id == int.Parse(d[0]) && m.id == int.Parse(d[1]))
                    {
                        ii = m;
                        break;
                    }
                }

                if (ii != null)
                {
                    sw = true;
                    ii.hp = int.Parse(d[2]);
                    ii.x = int.Parse(d[3]);
                    ii.y = int.Parse(d[4]);
                    ii.direction = int.Parse(d[5]);
                    ii.respawn = int.Parse(d[6]);
                }
                else
                {
                    sw = false;
                }
            }

            if(!sw)
            {
                Monster m = new Monster();
                m.map_id = int.Parse(d[0]);
                m.id = int.Parse(d[1]);
                m.hp = int.Parse(d[2]);
                m.x = int.Parse(d[3]);
                m.y = int.Parse(d[4]);
                m.direction = int.Parse(d[5]);
                m.respawn = int.Parse(d[6]);
                monster_data[m.map_id].Add(m);
            }
        }


        public void SaveItem(string data)
        {
            string[] d = data.Split(',');
            Item i = new Item();
            i.map_id = int.Parse(d[0]);
            i.id = int.Parse(d[1]);
            i.x = int.Parse(d[2]);
            i.y = int.Parse(d[3]);

            if (!item_data.ContainsKey(int.Parse(d[0])))
                item_data[int.Parse(d[0])] = new List<Item>();
            item_data[int.Parse(d[0])].Add(i);
        }

        public void DelItem(string data)
        {
            string[] s = data.Split(',');

            var d = from i in item_data[int.Parse(s[0])]
                    where (i.map_id == int.Parse(s[0]) && i.x == int.Parse(s[2]) && i.y == int.Parse(s[3]))
                    select i;

            item_data[int.Parse(s[0])].Remove(d.First());
        }

        public string SendMap(int id)
        {
            return map_data[id];
        }

        public List<string> respawnMonster()
        {
            List<string> list = new List<string>();
            foreach(var data in monster_data)
            {
                foreach(var d in data.Value)
                {
                    if (d.respawn > 0) d.respawn -= 60;
                    if (d.respawn < 0) d.respawn = 0;
                    if (d.respawn == 0)
                    {
                        // # 맵 id, 몹id, 몹 hp, x, y, 방향, 딜레이 시간
                        string s = d.map_id + "," + d.id + "," + d.x + "," + d.y + "," + d.direction;
                        list.Add(s);
                    }
                }
            }
            return list;
        }

        public void DelAllItem()
        {
            item_data.Clear();
        }

    }
}
