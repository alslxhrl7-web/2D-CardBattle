namespace CardBattle
{
    /// <summary>
    /// 한쪽 편의 군력(마나) 상태. 플레이어와 상대가 각자 하나씩 가진다.
    /// 규칙 수치(시작 군력, 최대치)는 GameRules에서 가져오므로 여기에는 계산만 있다.
    /// [Serializable]이라 인스펙터에서 현재 값을 볼 수 있다(확인용 — 게임 시작 시 Reset으로 덮어씀).
    /// </summary>
    [System.Serializable]
    public class ManaPool
    {
        public int current; // 지금 쓸 수 있는 군력
        public int max;     // 이번 턴의 최대 군력

        /// <summary>게임 시작 상태(1턴)로 되돌린다.</summary>
        public void Reset()
        {
            RefillForTurn(1);
        }

        /// <summary>해당 턴의 최대 군력으로 올리고, 현재 군력을 가득 채운다.</summary>
        public void RefillForTurn(int turnNumber)
        {
            max = GameRules.MaxManaForTurn(turnNumber);
            current = max;
        }

        /// <summary>이 비용을 낼 수 있는지만 확인한다(실제로 소모하지는 않음).</summary>
        public bool CanAfford(int cost)
        {
            return ClampCost(cost) <= current;
        }

        /// <summary>군력이 충분하면 소모하고 true, 부족하면 아무 것도 하지 않고 false.</summary>
        public bool TrySpend(int cost)
        {
            cost = ClampCost(cost);
            if (cost > current) return false; // 부족하면 실패
            current -= cost;
            return true;
        }

        /// <summary>음수 비용이 들어오면 0으로 본다(군력이 늘어나는 실수를 막기 위함).</summary>
        static int ClampCost(int cost)
        {
            return cost < 0 ? 0 : cost;
        }
    }
}
