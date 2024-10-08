﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SupremePlayServer
{
    public class Systemdata
    {
        public Dictionary<int, Dictionary<int, Monster>> monster_data; // 맵에 존재하는 몬스터의 데이터를 저장
        public Dictionary<int, Dictionary<int, EventNpc>> npc_data; // 맵에 존재하는 npc의 데이터를 저장
        public Dictionary<int, List<Item>> item_data2; // 맵에 존재하는 아이템의 데이터를 저장
        public Dictionary<int, string> map_data; // 맵의 이름 저장
        public Dictionary<int, int> party_quest_map_id;

        public Dictionary<string, int> packetMessageDict;
        public Dictionary<string, int> mapPacketMessageDict;
        public Dictionary<string, string> logMessageDict;

        public List<string> random_server_msg;

        public System_DB system_db;
        public mainForm mainForm;
        public Systemdata(mainForm mainForm)
        {
            try
            {
                this.mainForm = mainForm;
                var comparer = StringComparer.OrdinalIgnoreCase;
                monster_data = new Dictionary<int, Dictionary<int, Monster>>();
                item_data2 = new Dictionary<int, List<Item>>();
                npc_data = new Dictionary<int, Dictionary<int, EventNpc>>();
                map_data = new Dictionary<int, string>();

                packetMessageDict = new Dictionary<string, int>(comparer);
                mapPacketMessageDict= new Dictionary<string, int>(comparer);
                logMessageDict = new Dictionary<string, string>(comparer);
                party_quest_map_id = new Dictionary<int, int>();

                system_db = mainForm.systemDB;
                map_data = system_db.SendMap();
                random_server_msg = new List<string>();

                makePacketMessageDict(packetMessageDict);
                makeMapPacketMessageDict(mapPacketMessageDict);
                makeLogMessageDict(logMessageDict);
                make_random_msg(random_server_msg);
                makePartyQuestMapDict(party_quest_map_id); // 파티 퀘스트 맵 아이디 저장
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }

        public void make_random_msg(List<string> msg_list)
        {
            msg_list.Add("단축키 도움말은 F1키를 누르시면 볼 수 있습니다.");
            msg_list.Add("강력한 무기는 가끔씩 추가로 피해를 줄 수 있습니다.");
            msg_list.Add("부여성 남쪽의 해안가에서 파티퀘스트를 할 수 있습니다.");
            msg_list.Add("포목점에서 용의비늘을 감정할 수 있습니다.");
            msg_list.Add("놀이방의 피하기방에서 좋은 물건을 얻을 수 있을지도..");
            msg_list.Add("npc와의 대화는 마우스 클릭으로도 가능합니다.");
            msg_list.Add("죽었을 경우 마을의 성황당에서 부활할 수 있습니다.");
            msg_list.Add("강력한 적들은 원거리에서 공격할 수 도 있습니다.");
            msg_list.Add("\"/도움말\"을 입력하시면 명령어 도움말을 볼 수 있습니다.");
            msg_list.Add("주술사는 원거리에서 강력한 마법을 사용하는 직업입니다.");
            msg_list.Add("전사는 근거리에서 자신의 체력을 희생하여 강력한 한방을 주는 직업입니다.");
            msg_list.Add("도사는 파티원에게 이로운 마법을 걸어주거나 회복해줄 수 있는 직업입니다.");
            msg_list.Add("도적은 빠른 몸놀림으로 근거리에서 적을 상대하는 직업입니다.");
            msg_list.Add("레벨이 99가 되면 영혼사에서 경험치를 팔아서 체력, 마력을 살 수 있습니다.");
            msg_list.Add("푸줏간에선 고기를, 주막에선 술을 살 수 있습니다.");
            msg_list.Add("체력, 마력이 승급기준이 되면 부여 왕궁에서 승급 퀘스트를 진행 할 수 있습니다.");
            msg_list.Add("모든 직업의 승급은 4차까지 있습니다.");
            msg_list.Add("\"`\"키를 누르면 시스템 메뉴를 볼 수 있습니다.");
            msg_list.Add("부여성 북쪽으로 가면 세계전도로 갈 수 있습니다.");
            msg_list.Add("0~10, 20~30, 40~50분에는 선착장에서 일본으로 갈 수 있습니다.");
            msg_list.Add("10~20, 30~40, 50~60분에는 선착장에서 고균도로 갈 수 있습니다.");
            msg_list.Add("용무기는 극지방 대장간에서 은나무가지로 강화 할 수 있습니다.");
            msg_list.Add("일본, 중국 대장간에서 전설무기를 제작할 수 있습니다.");
            msg_list.Add("강력한 적들은 스스로 회복을 할 수 있지만 제한이 있습니다.");
            msg_list.Add("대화중 tap키를 눌러서 대화 타입을 변경할 수 있습니다.");
            msg_list.Add("아이템과 스킬은 마우스 오른쪽 버튼을 클릭해서 단축키 등록을 할 수 있습니다.");
            msg_list.Add("네이버 카페에서 다양한 공략과 소식을 볼 수 있습니다!");
        }

        public void makePacketMessageDict(Dictionary<string, int> dict)
        {
            // 0은 자신 제외, 1은 자신 포함
            // 메시지 관련
            dict.Add("<chat>", 1);    // 공지
            dict.Add("<chat1>", 1);   // 일반 채팅
            dict.Add("<bigsay>", 1);  // 외치기
            dict.Add("<System_Message>", 0);

            // 길드 관련
            dict.Add("<guild_message>", 1); // 길드 메시지
            dict.Add("<guild_group>", 0);
            dict.Add("<guild_invite>", 0);
            dict.Add("<guild_delete>", 0);

            // 운영자 권한 관련
            dict.Add("<ki>", 0);
            dict.Add("<summon>", 0);
            dict.Add("<all_summon>", 0);
            dict.Add("<prison>", 1);  // 감옥
            dict.Add("<emancipation>", 1);

            // 파티 관련
            dict.Add("<party>", 0);   // 파티
            dict.Add("<party_message>", 1);
            dict.Add("<party_req>", 0);
            dict.Add("<party_no>", 0);
            dict.Add("<party_yes>", 0);
            dict.Add("<party_out>", 0);

            // 교환 관련
            dict.Add("<trade_invite>", 0);
            dict.Add("<trade_system>", 0);
            dict.Add("<trade_item>", 0);
            dict.Add("<trade_money>", 0);
            dict.Add("<trade_okay>", 0);
            dict.Add("<trade_fail>", 0);

            // 기타
            dict.Add("<switches>", 0); // 스위치 공유
            dict.Add("<variables>", 0); // 변수 공유
            dict.Add("<8>", 0); // 유저 죽음 알림
        }

        public void makeMapPacketMessageDict(Dictionary<string, int> dict)
        {
            // 몬스터 관련
            dict.Add("<aggro>", 0); // 몬스터 어그로
            dict.Add("<respawn>", 0); // 몹 부활 공유
            dict.Add("<enemy_dead>", 0); // 몹 죽음 공유
            dict.Add("<mon_move>", 0); // 몬스터 이동 공유
            dict.Add("<mon_damage>", 0); // 몬스터 데미지 표시
            
            // 애니메이션 관련
            dict.Add("<event_animation>", 0);
            dict.Add("<player_animation>", 0);
            dict.Add("<27>", 0); // 애니메이션 재생

            // 파티 관련
            dict.Add("<party_heal>", 0); // 파티 힐
            dict.Add("<party_gain>", 0); // 파티 아이템 획득
            dict.Add("<party_move>", 0); // 파티 맵 이동

            // 플레이어 관련
            dict.Add("<player_damage>", 0); // 플레이어 데미지 표시
            dict.Add("<map_chat>", 0); // 플레이어 말풍선 표시

            // 아이템 드랍 관련
            dict.Add("<Drop>", 0);    // 템 드랍
            dict.Add("<Drop_Get>", 0);    // 템 삭제
            
            // pvp 관련
            dict.Add("<attack_effect>", 0); // pvp 평타
            dict.Add("<skill_effect>", 0); // pvp 스킬

            // 기타
            dict.Add("<se_play>", 0); // 효과음 실행
            dict.Add("<monster_chat>", 0); // 몬스터 말풍선 표시
            dict.Add("<show_range_skill>", 0); // 스킬 
            dict.Add("<make_range_sprite>", 0); // 범위 스킬 이펙트
        }

        // 로그에 넣을 메시지
        public void makeLogMessageDict(Dictionary<string, string> dict)
        {
            string tag = "";
            // 플레이어 관련
            dict.Add("<login>", "");
            //dict.Add("<m5>", 0);
            dict.Add("<9>", ""); // 플레이어 종료
            // 기타
            dict.Add("<map_name>", ""); // 맵 이름

            // 메시지 관련
            tag = "chat";
            dict.Add("<System_Message>", tag);
            dict.Add("<chat>", tag);   // 공지
            dict.Add("<chat1>", tag);   // 일반 채팅
            dict.Add("<bigsay>", tag);  // 외치기
            dict.Add("<whispers>", tag); // 귓속말
            dict.Add("<party_message>", tag); // 파티

            // 운영자 권한 관련
            tag = "admin_authority";
            dict.Add("<summon>", tag);
            dict.Add("<all_summon>", tag);
            dict.Add("<prison>", tag);  // 감옥
            dict.Add("<cashgive>", tag);

            // 파티 관련
            tag = "party";
            dict.Add("<party_create>", tag);   // 파티
            dict.Add("<party_end>", tag);
            dict.Add("<party_invite>", tag);
            dict.Add("<party_accept>", tag);

            // 교환 관련
            tag = "trade";
            dict.Add("<trade_invite>", tag);
            dict.Add("<trade_addItem>", tag);
            dict.Add("<trade_removeItem>", tag);
            dict.Add("<trade_ready>", tag);
            dict.Add("<trade_cancel>", tag);
            dict.Add("<trade_accept>", tag);

            // 중요 로그
            tag = "important_log";
            //dict.Add("<item_log>", $"{tag}//item");    // 아이템
            //dict.Add("<status_log>", $"{tag}//status");    // 스텟
            //dict.Add("<make_log>", $"{tag}//make");    // 제작

            // 아이템 드랍 관련
            //dict.Add("<Drop>", "");    // 템 드랍
            //dict.Add("<Drop_Get>", "");    // 템 줍기
        }

        public void makePartyQuestMapDict(Dictionary<int, int> dict)
        {
            dict.Add(51, 1015);
            dict.Add(113, 1143);
            dict.Add(404, 1152);
        }


        public void SaveMonster(string data)
        {
            try
            {
                var d = system_db.ParseKeyValueData(data);
                int map_id = int.Parse(d["map_id"]);
                int id = int.Parse(d["id"]);
                Monster monster;
                if (!monster_data.ContainsKey(map_id))
                {
                    monster_data[map_id] = new Dictionary<int, Monster>();
                }

                if (!monster_data[map_id].ContainsKey(id))
                {
                    monster = new Monster();
                    monster_data[map_id][id] = monster;
                }

                monster = monster_data[map_id][id];
                // 초기에만 저장할 것
                if (monster.id == 0)
                {
                    int.TryParse(d["id"], out monster.id);
                    int.TryParse(d["map_id"], out monster.map_id);
                    int.TryParse(d["mon_id"], out monster.mon_id);
                    int.TryParse(d["respawn"], out monster.respawn);
                    int.TryParse(d["delete_sw"], out int delete_sw_value);
                    monster.delete_sw = delete_sw_value != 0;
                    monster.respawn_save = monster.respawn;
                }

                // 변동 변수들
                long.TryParse(d["hp"], out monster.hp);
                int.TryParse(d["sp"], out monster.sp);
                int.TryParse(d["direction"], out monster.direction);                
                int.TryParse(d["x"], out monster.x);
                int.TryParse(d["y"], out monster.y);

                if(d.ContainsKey("buffTime"))
                {
                    //monster.buffTime
                }

                monster.dead = false;
                if(monster.hp <= 0)
                {
                    monster.dead = true;
                }
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }

        public void DeleteMonster(string data, int map_id)
        {
            try
            {
                int id = int.Parse(data);
                if (!monster_data.ContainsKey(map_id)) return;
                if (!monster_data[map_id].ContainsKey(id)) return;

                monster_data[map_id][id].dead = true;
                if (monster_data[map_id][id].delete_sw)
                {
                    monster_data[map_id].Remove(id);
                    return;
                }
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }

        public void SaveNpc(string data)
        {
            try
            {
                var d = system_db.ParseKeyValueData(data);
                int map_id = int.Parse(d["map_id"]);
                int id = int.Parse(d["id"]);

                EventNpc npc;
                if (!npc_data.ContainsKey(map_id))
                {
                    npc_data[map_id] = new Dictionary<int, EventNpc>();
                }

                if (!npc_data[map_id].ContainsKey(id))
                {
                    npc = new EventNpc();
                    npc_data[map_id][id] = npc;
                }

                npc = npc_data[map_id][id];
                // 초기에만 저장할 것
                if (npc.id == 0)
                {
                    int.TryParse(d["id"], out npc.id);
                    int.TryParse(d["map_id"], out npc.map_id);
                    int.TryParse(d["npc_id"], out npc.npc_id);
                    int.TryParse(d["x"], out npc.x);
                    int.TryParse(d["y"], out npc.y);
                    int.TryParse(d["direction"], out npc.direction);
                }
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }

        public void DeleteNpc(string data)
        {
            try
            {
                var d = system_db.ParseKeyValueData(data);
                int map_id = int.Parse(d["map_id"]);
                int id = int.Parse(d["id"]);

                if (!npc_data.ContainsKey(map_id)) return;
                if (!npc_data[map_id].ContainsKey(id)) return;

                npc_data[map_id].Remove(id);
                return;
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }


        public void SaveItem(string data)
        {
            try
            {
                var d = system_db.ParseKeyValueData(data);
                var i = new Item
                {
                    id = int.Parse(d["id"]),
                    type = int.Parse(d["type"]),
                    map_id = int.Parse(d["map_id"]),
                    x = int.Parse(d["x"]),
                    y = int.Parse(d["y"]),
                    num = int.Parse(d["num"]),
                    sw = int.Parse(d["sw"]),
                    item_id = int.Parse(d["item_id"])
                };

                if (!item_data2.TryGetValue(i.map_id, out var itemList))
                {
                    itemList = new List<Item>();
                    item_data2[i.map_id] = itemList;
                }

                itemList.Add(i);
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }

        public void DelItem2(string data, int map_id) // 맵id, id
        {
            try
            {
                string[] s = data.Split(',');
                int id = int.Parse(s[0]);

                if (!item_data2.ContainsKey(map_id)) return;

                var d = from i in item_data2[map_id]
                        where i.id == id
                        select i;

                if (d != null && d.Count() > 0)
                    item_data2[map_id].Remove(d.First());
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
            }
        }

        public async System.Threading.Tasks.Task<string> SendMapAsync(int id)
        {
            try
            {
                if (map_data.ContainsKey(id)) return map_data[id];
                else
                {
                    map_data = system_db.SendMap();
                    if (map_data.ContainsKey(id)) return map_data[id];
                    else return "null";
                }
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
                return "null";
            }
        }

        public List<string> respawnMonster2()
        {
            try
            {
                List<string> list = new List<string>();
                var d = from i in monster_data.Values
                        from ii in i.Values
                        where (ii.respawn > 0 && ii.dead)
                        select ii;

                foreach (var data in d)
                {
                    data.respawn -= 1;
                    if (data.respawn <= 0)
                    {
                        data.respawn = data.respawn_save;

                        List<string> dataList = new List<string>();
                        dataList.Add(data.map_id.ToString());
                        dataList.Add(data.id.ToString());
                        dataList.Add(data.mon_id.ToString());

                        string s = string.Join(",", dataList);
                        data.dead = false;
                        list.Add(s);
                    }
                }

                return list;
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
                return null;
            }
        }

        public List<int> aggroTimeMonster(int map_id)
        {
            try
            {
                List<int> list = new List<int>();
                var selectData = from monster in monster_data.Values
                        from m in monster.Values
                        where (m.map_id == map_id && m.aggroTime > 0)
                        select m;

                foreach (var data in selectData)
                {
                    data.aggroTime -= 1;
                    if (data.aggroTime <= 0)
                    {
                        data.aggroTime = data.aggroResetTime;
                        list.Add(data.id);
                    }
                }

                return list;
            }
            catch (Exception e)
            {
                mainForm.write_log(e.ToString());
                return null;
            }
        }

        public void DelAllItem()
        {
            item_data2.Clear();
        }

        public int[] checkPartyQuest(int map_id)
        {
            int[] data = new int[2]; // sw_id, on/off
            try
            {
                if (!party_quest_map_id.ContainsKey(map_id)) return data;

                data[0] = party_quest_map_id[map_id]; // 스위치 id
                if (!mainForm.MapUser2.ContainsKey(map_id)) data[1] = 0;
                else if (mainForm.MapUser2[map_id].Count <= 0) data[1] = 0;
                else data[1] = 1; // 만약 해당 맵에 사람이 있다면 파티 퀘스트 체크 스위치 
            }
            catch(Exception e)
            {
                mainForm.write_log(e.ToString());
            }
            return data;
        }
    }
}
