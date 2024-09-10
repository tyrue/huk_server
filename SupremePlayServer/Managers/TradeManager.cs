using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupremePlayServer
{
    public class TradeManager
    {
        public List<Item> myItems;
        public List<Item> traderItems;
        public int maxSize;
        public UserThread user;
        public UserThread trader;
        public bool isReady;

        public TradeManager(UserThread user)
        {
            myItems = new List<Item>();
            traderItems = new List<Item>();
            maxSize = 5;
            this.user = user ?? throw new ArgumentNullException(nameof(user), "User cannot be null");

            isReady = false;
        }

        public async Task<bool> AddItemAsync(Dictionary<string, string> data)
        {
            if (myItems.Count >= maxSize)
            {
                await user.SendConsoleMessageAsync("남은 자리가 없습니다.");
                return false;
            }

            if (data == null || !data.ContainsKey("id") || !data.ContainsKey("num") || !data.ContainsKey("type"))
            {
                await user.SendConsoleMessageAsync("아이템 추가 데이터가 올바르지 않습니다.");
                return false;
            }

            Item item = new Item();
            int.TryParse(data["id"], out item.item_id);
            int.TryParse(data["num"], out item.num);
            int.TryParse(data["type"], out item.type);
            if (trader?.tradeManager?.traderItems != null)
            {
                myItems.Add(item);
                return true;
            }

            await user.SendConsoleMessageAsync("교환 상대방의 정보를 가져오지 못했습니다.");
            return false;
        }

        public async Task AddItemToTraderAsync()
        {
            Item item = myItems.Last();
            trader.tradeManager.traderItems.Add(item);
            string message = $"id:{item.item_id}|num:{item.num}|type:{item.type}";
            await trader.SendMessageWithTagAsync("trade_add", message);
        }


        public async Task RemoveItemAsync(int index)
        {
            if (index < 0 || index >= myItems.Count) return;

            myItems.RemoveAt(index);

            if (trader?.tradeManager?.traderItems != null && index < trader.tradeManager.traderItems.Count)
            {
                trader.tradeManager.traderItems.RemoveAt(index);
                await trader.SendMessageWithTagAsync("trade_remove", index.ToString());
            }
        }

        public async Task ReadyTradeAsync()
        {
            isReady = true;

            if (trader == null)
            {
                await user.SendConsoleMessageAsync("교환 상대방이 없습니다.");
                return;
            }

            await trader.SendConsoleMessageAsync($"{user.userName}님이 준비가 완료되었습니다.");

            if (trader.tradeManager?.isReady == true)
            {
                await trader.tradeManager.SuccessTradeAsync();
                await SuccessTradeAsync();
            }
        }

        public async Task SuccessTradeAsync()
        {
            string message = "";
            if (traderItems?.Count > 0)
            {
                message = string.Join(",", traderItems.ConvertAll(item =>
                    $"id:{item.item_id}|type:{item.type}|num:{item.num}"
                ));
            }

            await user.SendMessageWithTagAsync("trade_success", message);
            await user.SendConsoleMessageAsync("교환에 성공했습니다.");
            InitialSetting();
        }

        public async Task CancelTradeAsync()
        {
            if (trader != null) await trader.tradeManager.CancelProcessAsync();
            await CancelProcessAsync();
        }

        public async Task CancelProcessAsync()
        {
            await user.SendMessageWithTagAsync("trade_cancel", "");
            await user.SendConsoleMessageAsync("교환이 취소 되었습니다.");
            InitialSetting();
        }

        public async Task InviteTradeAsync(string name)
        {
            if (trader != null)
            {
                await user.SendConsoleMessageAsync("[교환]:이미 교환 중입니다.");
                return;
            }
            if (string.IsNullOrEmpty(name))
            {
                await user.SendConsoleMessageAsync("[교환]:초대할 유저의 이름을 입력하세요.");
                return;
            }
            if (name.Equals(user.userName))
            {
                await user.SendConsoleMessageAsync("[교환]:자기 자신을 초대할 수 없습니다.");
                return;
            }
            if (!user.mainForm.UserByNameDict.ContainsKey(name))
            {
                await user.SendConsoleMessageAsync($"[교환]:{name}님은 현재 존재하지 않습니다.");
                return;
            }

            trader = user.mainForm.UserByNameDict[name];
            trader.tradeManager.trader = user;
            await trader.SendMessageWithTagAsync("trade_invite", $"{user.userName}");
            await user.SendConsoleMessageAsync($"{name}님에게 교환 신청을 하였습니다.");
        }

        public async Task AcceptTradeAsync()
        {
            if (trader == null)
            {
                await user.SendConsoleMessageAsync("교환 상대방이 없습니다.");
                return;
            }

            await trader.SendMessageWithTagAsync("trade_start", "");
            await user.SendMessageWithTagAsync("trade_start", "");
        }

        public async Task RefuseTradeAsync()
        {
            if (trader == null) return;

            await trader.tradeManager?.RefuseProcessAsync(user.userName);
            await CancelProcessAsync();
        }

        public async Task RefuseProcessAsync(string name)
        {
            await user.SendConsoleMessageAsync($"{name}님께서 교환을 거절하셨습니다.");
            await user.SendMessageWithTagAsync("trade_cancel", "");
            InitialSetting();
        }

        public void InitialSetting()
        {
            myItems.Clear();
            traderItems.Clear();
            trader = null;
            isReady = false;
        }
    }
}
