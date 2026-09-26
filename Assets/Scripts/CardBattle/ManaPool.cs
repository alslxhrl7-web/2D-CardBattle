namespace CardBattle
{
    /// <summary>
    /// 한쪽 편의 군력(마나) 상태. 플레이어와 상대가 각자 하나씩 가진다.
    /// 규칙 수치(시작 군력, 최대치)는 GameRules에서 가져오므로 여기에는 계산 로직만 있다.
    /// [Serializable]이라 인스펙터에서 현재 값을 확인할 수 있다(디버깅용 — 게임 시작 시 Reset으로 덮어씀).
    /// </summary>
    [System.Serializable]
    public class ManaPool
    {
        public int current;
        public int max;

        /// <summary>게임 시작 상태(1턴)로 되돌린다.</summary>
        public void Reset()
        {
            RefillForTurn(1);
        }

        /// <summary>해당 턴의 최대 군력으로 올리고 현재 군력을 가득 채운다.</summary>
        public void RefillForTurn(int turnNumber)
        {
            max = GameRules.MaxManaForTurn(turnNumber);
            current = max;
        }

        public bool CanAfford(int cost)
        {
            return ClampCost(cost) <= current;
        }

        /// <summary>군력이 충분하면 소모하고 true, 부족하면 아무 것도 하지 않고 false.</summary>
        public bool TrySpend(int cost)
        {
            cost = ClampCost(cost);
            if (cost > current) return false;
            current -= cost;
            return true;
        }

        static int ClampCost(int cost)
        {
            return cost < 0 ? 0 : cost;
        }
    }
}
