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
            if (u1 == null || u2 == null)
                throw new ArgumentNullException("UserThread 객체가 null입니다.");
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
            if (user == null)
                throw new ArgumentNullException(nameof(user), "UserThread 객체가 null일 수 없습니다.");


            partyMembers = new SortedSet<UserThread>(new Comp());
            maxSize = 5;
            myUser = user;
        }

        public async Task<bool> CreatePartyAsync()
        {
            if (partyMembers.Count > 0)
            {
                await myUser.SendConsoleMessageAsync("이미 파티가 있습니다.");
                return false;
            }

            leader = myUser;
            await myUser.SendConsoleMessageAsync("파티가 생성되었습니다.");
            await AddProcessAsync(myUser);
            return true;
        }

        public async Task<bool> AddMemberAsync(UserThread user)
        {
            if (user == null)
            {
                await myUser.SendConsoleMessageAsync("유효하지 않은 사용자입니다.");
                return false;
            }

            if (partyMembers.Count >= maxSize)
            {
                await myUser.SendConsoleMessageAsync("남은 자리가 없습니다.");
                return false;
            }

            foreach (var member in partyMembers)
            {
                if (!member.Equals(myUser))
                {
                    await member.partyManger.AddProcessAsync(user);
                }
            }
            await AddProcessAsync(user);
            return true;
        }

        public async Task AddProcessAsync(UserThread user)
        {
            if (user == null) return;

            partyMembers.Add(user);
            await myUser.SendConsoleMessageAsync($"{user.userName}님을 파티에 추가했습니다.");
            await myUser.SendMessageWithTagAsync("party_add", $"member:{user.userName}");
        }

        public async Task RemoveMemberAsync(UserThread user)
        {
            if (user == null || !partyMembers.Contains(user)) return;

            await RemoveProcessAsync(user);
            foreach (var member in partyMembers)
            {
                if (member != null && !member.Equals(myUser))
                {
                    await member.partyManger?.RemoveProcessAsync(user);
                }
            }
        }

        public async Task RemoveProcessAsync(UserThread user)
        {
            if (user == null) return;

            partyMembers.Remove(user);
            await myUser.SendConsoleMessageAsync($"{user.userName}님이 파티를 탈퇴 했습니다.");
            await myUser.SendMessageWithTagAsync("party_remove", $"member:{user.userName}");

            if (leader != null && leader.Equals(user) && partyMembers.Count > 0)
            {
                await SetLeaderAsync(partyMembers.First());
            }
        }

        public async Task EndPartyAsync()
        {
            partyMembers.Remove(myUser);
            foreach (var member in partyMembers)
            {
                await member?.partyManger?.RemoveMemberAsync(myUser);
            }

            await myUser.SendConsoleMessageAsync("파티를 탈퇴 했습니다.");
            await InitialSetting();
        }

        public async Task InvitePartyAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                await myUser.SendConsoleMessageAsync("[파티]:초대할 유저의 이름을 입력하세요.");
                return;
            }
            if (name.Equals(myUser.userName))
            {
                await myUser.SendConsoleMessageAsync("[파티]:자기 자신을 초대할 수 없습니다.");
                return;
            }
            if (partyMembers.Count >= maxSize)
            {
                await myUser.SendConsoleMessageAsync($"[파티]:파티는 최대 {maxSize}명 까지 가능합니다.");
                return;
            }
            if (!myUser.mainForm.UserByNameDict.ContainsKey(name))
            {
                await myUser.SendConsoleMessageAsync($"[파티]:{name}님은 현재 존재하지 않습니다.");
                return;
            }

            if (partyMembers.Count <= 0)
            {
                await CreatePartyAsync();
            }

            var target = myUser.mainForm.UserByNameDict[name];
            target.partyManger.inviter = myUser;

            string msg = $"name:{myUser.userName}";
            await target.SendMessageWithTagAsync("party_req", msg);
        }

        public async Task AcceptPartyAsync()
        {
            if (inviter == null) return;

            var target = inviter.partyManger;
            if (await target.AddMemberAsync(myUser))
            {
                await EnterPartyAsync(target.partyMembers, target.leader);
                inviter = null;
            }
        }

        public async Task RefusePartyAsync()
        {
            if (inviter == null) return;

            await inviter.SendConsoleMessageAsync($"{myUser.userName}님이 초대를 거절하셨습니다.");
            await inviter.partyManger?.InitialSetting();
            await InitialSetting();
        }

        public async Task EnterPartyAsync(SortedSet<UserThread> members, UserThread leader)
        {
            foreach (var member in members)
            {
                await AddProcessAsync(member);
            }
            this.leader = leader;
            await myUser.SendConsoleMessageAsync("파티에 참가하였습니다.");
        }

        public async Task SetLeaderAsync(UserThread leader)
        {
            this.leader = leader;
            await myUser.SendConsoleMessageAsync($"{leader.userName}님이 파티장이 되셨습니다.");
        }

        public async Task InitialSetting()
        {
            partyMembers.Clear();
            leader = null;
            inviter = null;
        }
    }
}
