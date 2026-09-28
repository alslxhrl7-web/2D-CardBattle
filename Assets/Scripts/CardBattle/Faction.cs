namespace CardBattle
{
    /// <summary>카드가 속한 진영. 조선(수성)과 청(기동전)이 현재 메인이고, 명나라는 시즌2 확장용으로 예약된 값.</summary>
    public enum Faction
    {
        Joseon, // 조선: 성벽·방어 중심
        Qing,   // 청(후금): 기병·속도 중심
        Ming    // 명나라: 확장용(아직 카드 없음)
    }

    /// <summary>카드의 큰 분류.</summary>
    public enum CardKind
    {
        Unit,      // 장수(유닛): 전열 빈 칸에 내는 카드
        Spell,     // 전술: 보드 위로 끌어다 놓으면 즉시 효과(spellEffect)가 나고 사라지는 카드
        Weapon,    // 장비: 내 후열 칸에 놓으면 같은 레인 전열 카드의 공격력/체력이 오르는 카드
        Formation  // 진(陣): 전열 빈 칸에 내는 구조물. 공격력 0이면 공격하지 않고 막기만 한다
    }

    /// <summary>
    /// 전술(Spell) 카드를 쓸 때, 또는 유닛·진을 낼 때(등장 효과) 일어나는 효과. 효과의 크기는 CardData.effectValue.
    /// 새 효과를 만들려면 여기에 이름을 추가하고 CardManager.ApplySpell()에 처리를 한 줄 넣으면 된다.
    /// </summary>
    public enum SpellEffect
    {
        None,                 // 효과 없음
        DamageEnemyHero,      // 상대 히어로에게 effectValue 피해
        DamageAllEnemyUnits,  // 상대 필드의 모든 카드에 effectValue 피해
        BuffAllAllies,        // 내 필드의 모든 카드 공격력/체력 +effectValue
        DrawCards,            // 카드 effectValue장 드로우
        HealHero              // 내 히어로 체력 effectValue 회복 (시작 체력을 넘지 않음)
    }

    /// <summary>
    /// 카드 희귀도. 값이 클수록 높은 등급이다.
    /// 카드팩의 "이 등급 이상" 비교에 이 순서를 그대로 쓰므로 순서를 바꾸지 말 것.
    /// </summary>
    public enum Rarity
    {
        Common,    // 일반: 리본 표시 없음, 덱에 최대 3장
        Elite,     // 엘리트: 덱에 최대 2장
        Legendary, // 전설: 덱에 최대 1장
        Hero       // 히어로: 덱에 최대 1장
    }

    /// <summary>
    /// 대전의 한쪽 편. 플레이어/상대가 똑같이 하는 일(군력, 체력, 드로우 등)을
    /// "플레이어면 ~, 아니면 ~"로 두 번 쓰지 않고 Side 하나로 처리하기 위해 사용한다.
    /// </summary>
    public enum Side
    {
        Player, // 플레이어(조선, 화면 아래쪽)
        Enemy   // 상대(청, 화면 위쪽 — AI 대전에서는 AI, 2인 대전에서는 두 번째 사람)
    }

    /// <summary>한 판을 어떤 방식으로 하는지. 처음 화면의 버튼마다 다르다.</summary>
    public enum GameMode
    {
        VsAI,      // 배틀 시작: 청은 AI가 둔다
        TwoPlayer, // 2인 대전: 방을 만들어 방 번호로 친구와 대전 (서버는 온라인과 같음)
        Tutorial,  // 튜토리얼: 정해진 덱으로 안내를 따라 한 판
        Online     // 온라인 대전: 서버로 다른 컴퓨터의 사람과 (OnlineMatch.cs)
    }

    /// <summary>Side에 붙여 쓰는 편의 함수 모음.</summary>
    public static class SideExtensions
    {
        /// <summary>반대편을 돌려준다. (Player ↔ Enemy)</summary>
        public static Side Opponent(this Side side)
        {
            return side == Side.Player ? Side.Enemy : Side.Player;
        }
    }
}
