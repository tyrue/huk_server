using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupremePlayServer
{
    public class PartyManger
    {
        public SortedSet<string> partyMembers;
        public string leader;
        public int maxSize;
        public UserThread user;
        public string inviteMember;

        public PartyManger(UserThread user)
        {
            partyMembers = new SortedSet<string>();
            maxSize = 5;
            this.user = user;
        }

        public bool createParty()
        {
            if (partyMembers.Count > 0)
            {
                user.SendConsoleMessage("이미 파티가 있습니다.");
                return false;
            }

            if (!addMember(user.userName)) return false;

            user.SendConsoleMessage("파티가 생성되었습니다.");
            leader = user.userName;
            return true;
        }

        public bool addMember(string name)
        {
            if (partyMembers.Count >= maxSize)
            {
                user.SendConsoleMessage("남은 자리가 없습니다.");
                return false;
            }

            partyMembers.Add(name);
            user.SendConsoleMessage($"{name}님을 파티에 추가했습니다.");
            user.SendMessageWithTag("party_add", $"member:{name}");
            return true;
        }

        public void removeMember(string name)
        {
            if (!partyMembers.Contains(name)) return;
            partyMembers.Remove(name);
            user.SendConsoleMessage($"{name}님이 파티를 탈퇴 했습니다.");
            user.SendMessageWithTag("party_remove", $"member:{name}");

            if (!leader.Equals(name)) return;
            if (partyMembers.Count <= 0) return;
            setLeader(partyMembers.First());
        }

        public void endParty()
        {
            partyMembers.Remove(user.userName);
            foreach(var member in partyMembers)
            {
                if (!user.mainForm.UserByNameDict.ContainsKey(member)) continue;
                var mem = user.mainForm.UserByNameDict[member];
                mem.partyManger.removeMember(user.userName);
            }
            user.SendConsoleMessage("파티를 탈퇴 했습니다.");
            partyMembers.Clear();
            leader = "";
        }

        public void inviteParty(string name) // 파티 초대
        {
            if(string.IsNullOrEmpty(name))
            {
                user.SendConsoleMessage("[파티]:초대할 유저의 이름을 입력하세요.");
                return;
            }
            if (name.Equals(user.userName))
            {
                user.SendConsoleMessage("[파티]:자기 자신을 초대할 수 없습니다.");
                return;
            }
            if (partyMembers.Count >= maxSize)
            {
                user.SendConsoleMessage($"[파티]:파티는 최대 {maxSize}명 까지 가능합니다.");
                return;
            }
            if(!user.mainForm.UserByNameDict.ContainsKey(name))
            {
                user.SendConsoleMessage($"[파티]:{name}님은 현재 존재하지 않습니다.");
                return;
            }

            if (partyMembers.Count <= 0) createParty();

            inviteMember = name;
            var target = user.mainForm.UserByNameDict[name];
            string msg = $"name:{user.userName}";
            target.SendMessageWithTag("party_req", msg);
        }

        public void acceptParty(string name)
        {
            if (!inviteMember.Equals(name)) return;
            var target = user.mainForm.UserByNameDict[name];
            target.partyManger.enterParty(partyMembers, leader);
            addMember(name);
            inviteMember = "";
        }

        public void refuseParty(string name)
        {
            if (!inviteMember.Equals(name)) return;
            user.SendConsoleMessage($"{name}님이 초대를 거절하셨습니다.");
            inviteMember = "";
        }

        public void enterParty(SortedSet<string> members, string leader)
        {
            foreach (var member in members)
            {
                addMember(member);
            }
            addMember(user.userName);
            this.leader = leader;
            user.SendConsoleMessage("파티에 참가하였습니다.");
        }

        public void setLeader(string leader)
        {
            this.leader = leader;
            user.SendConsoleMessage($"{leader}님이 파티장이 되셨습니다.");
        }

    }
}
