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
            this.user = user;
            isReady = false;
        }

        public bool addItem(Dictionary<string, string> data)
        {
            if (myItems.Count >= maxSize)
            {
                user.SendConsoleMessage("남은 자리가 없습니다.");
                return false;
            }

            Item item = new Item();
            int.TryParse(data["id"], out item.item_id);
            int.TryParse(data["num"], out item.num);
            int.TryParse(data["type"], out item.type);

            myItems.Add(item);
            trader.tradeManager.traderItems.Add(item);

            string message = $"id:{item.item_id}";
            message += $"|num:{item.num}";
            message += $"|type:{item.type}";

            trader.SendMessageWithTag("trade_add", message);
            return true;
        }

        public void removeItem(int index)
        {
            if (myItems.Count() < index) return;
            myItems.RemoveAt(index);
            trader.tradeManager.traderItems.RemoveAt(index);
            trader.SendMessageWithTag("trade_remove", index.ToString());
        }

        public void readyTrade()
        {
            isReady = true;
            trader.SendConsoleMessage($"{user.userName}님이 준비가 완료되었습니다.");
            if (trader.tradeManager.isReady)
            {
                trader.tradeManager.successTrade();
                successTrade();
            }
        }

        public void successTrade()
        {
            string message = "";
            foreach (var item in traderItems)
            {
                message += $"id:{item.item_id}";
                message += $"|type:{item.type}";
                message += $"|num:{item.num},";
            }
            user.SendMessageWithTag("trade_success", message);
            user.SendConsoleMessage("교환에 성공했습니다.");
            initialSetting();
        }

        public void cancelTrade()
        {
            if(trader != null) trader.tradeManager.cancelProcess();
            cancelProcess();
        }

        public void cancelProcess()
        {
            user.SendMessageWithTag("trade_cancel", "");
            user.SendConsoleMessage("교환이 취소 되었습니다.");
            initialSetting();
        }

        public void inviteTrade(string name) // 교환 초대
        {
            if(trader != null)
            {
                user.SendConsoleMessage("[교환]:이미 교환 중입니다.");
                return;
            }
            if(string.IsNullOrEmpty(name))
            {
                user.SendConsoleMessage("[교환]:초대할 유저의 이름을 입력하세요.");
                return;
            }
            if (name.Equals(user.userName))
            {
                user.SendConsoleMessage("[교환]:자기 자신을 초대할 수 없습니다.");
                return;
            }
            if(!user.mainForm.UserByNameDict.ContainsKey(name))
            {
                user.SendConsoleMessage($"[교환]:{name}님은 현재 존재하지 않습니다.");
                return;
            }
            trader = user.mainForm.UserByNameDict[name];
            trader.tradeManager.trader = user;
            trader.SendMessageWithTag("trade_invite", $"{user.userName}");
            user.SendConsoleMessage($"{name}님에게 교환 신청을 하였습니다.");
        }

        public void acceptTrade()
        {
            trader.SendMessageWithTag("trade_start");
            user.SendMessageWithTag("trade_start");
        }

        public void refuseTrade()
        {
            if (trader == null) return;
            trader.tradeManager.refuseProcess(user.userName);
            cancelProcess();
        }

        public void refuseProcess(string name)
        {
            user.SendConsoleMessage($"{name}님께서 교환을 거절하셨습니다.");
            user.SendMessageWithTag("trade_cancel", "");
            initialSetting();
        }

        public void initialSetting()
        {
            myItems.Clear();
            traderItems.Clear();
            trader = null;
            isReady = false;
        }

    }
}
