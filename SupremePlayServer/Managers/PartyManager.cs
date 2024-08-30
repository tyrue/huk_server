using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupremePlayServer
{
    public class Comp:IComparer<UserThread>
    {
        public int Compare(UserThread u1, UserThread u2)
        {
            return u1.userName.CompareTo(u2.userName);
        }

    }

    public class PartyManager
    {
        public SortedSet<UserThread> partyMembers;
        public UserThread leader;
        public int maxSize;
        public UserThread myUser;
        public UserThread inviter;

        public PartyManager(UserThread user)
        {
            partyMembers = new SortedSet<UserThread>(new Comp());
            maxSize = 5;
            myUser = user;
        }

        public bool createParty()
        {
            if (partyMembers.Count > 0)
            {
                myUser.SendConsoleMessage("이미 파티가 있습니다.");
                return false;
            }

            leader = myUser;
            myUser.SendConsoleMessage("파티가 생성되었습니다.");
            addProcess(myUser);
            return true;
        }

        public bool addMember(UserThread user)
        {
            if (partyMembers.Count >= maxSize)
            {
                myUser.SendConsoleMessage("남은 자리가 없습니다.");
                return false;
            }

            foreach(var member in partyMembers)
            {
                if(!member.Equals(myUser)) member.partyManger.addProcess(user);
            }
            addProcess(user);
            return true;
        }

        public void addProcess(UserThread user)
        {
            partyMembers.Add(user);
            myUser.SendConsoleMessage($"{user.userName}님을 파티에 추가했습니다.");
            myUser.SendMessageWithTag("party_add", $"member:{user.userName}");
        }

        public void removeMember(UserThread user)
        {
            if (!partyMembers.Contains(user)) return;

            removeProcess(user);
            foreach(var member in partyMembers)
            {
                if (!member.Equals(myUser)) member.partyManger.removeProcess(user);
            }
        }

        public void removeProcess(UserThread user)
        {
            partyMembers.Remove(user);
            myUser.SendConsoleMessage($"{user.userName}님이 파티를 탈퇴 했습니다.");
            myUser.SendMessageWithTag("party_remove", $"member:{user.userName}");

            if (!leader.Equals(user)) return;
            if (partyMembers.Count <= 0) return;
            setLeader(partyMembers.First());
        }

        public void endParty()
        {
            partyMembers.Remove(myUser);
            foreach(var member in partyMembers)
            {
                member.partyManger.removeMember(myUser);
            }
            myUser.SendConsoleMessage("파티를 탈퇴 했습니다.");
            initialSetting();
        }

        public void inviteParty(string name) // 파티 초대
        {
            if(string.IsNullOrEmpty(name))
            {
                myUser.SendConsoleMessage("[파티]:초대할 유저의 이름을 입력하세요.");
                return;
            }
            if (name.Equals(myUser.userName))
            {
                myUser.SendConsoleMessage("[파티]:자기 자신을 초대할 수 없습니다.");
                return;
            }
            if (partyMembers.Count >= maxSize)
            {
                myUser.SendConsoleMessage($"[파티]:파티는 최대 {maxSize}명 까지 가능합니다.");
                return;
            }
            if(!myUser.mainForm.UserByNameDict.ContainsKey(name))
            {
                myUser.SendConsoleMessage($"[파티]:{name}님은 현재 존재하지 않습니다.");
                return;
            }

            if (partyMembers.Count <= 0) createParty();

            var target = myUser.mainForm.UserByNameDict[name];
            target.partyManger.inviter = myUser;

            string msg = $"name:{myUser.userName}";
            target.SendMessageWithTag("party_req", msg);
        }

        public void acceptParty()
        {
            if (inviter == null) return;
            var target = inviter.partyManger;
            target.addMember(myUser);
            enterParty(target.partyMembers, target.leader);
            inviter = null;
        }

        public void refuseParty()
        {
            if (inviter == null) return;
            inviter.SendConsoleMessage($"{myUser.userName}님이 초대를 거절하셨습니다.");
            inviter.partyManger.initialSetting();
            initialSetting();
        }

        public void enterParty(SortedSet<UserThread> members, UserThread leader)
        {
            foreach (var member in members)
            {
                addProcess(member);
            }
            this.leader = leader;
            myUser.SendConsoleMessage("파티에 참가하였습니다.");
        }

        public void setLeader(UserThread leader)
        {
            this.leader = leader;
            myUser.SendConsoleMessage($"{leader.userName}님이 파티장이 되셨습니다.");
        }

        public void initialSetting()
        {
            partyMembers.Clear();
            leader = null;
            inviter = null;
        }
    }
}
