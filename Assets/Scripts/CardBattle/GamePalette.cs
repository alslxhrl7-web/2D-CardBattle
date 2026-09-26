using UnityEngine;

namespace CardBattle
{
    /// <summary>버튼 하나의 상태별 배경색(평소 / 마우스 올림 / 누르는 중).</summary>
    public struct ButtonColors
    {
        public Color normal; // 평소 색
        public Color hover;  // 마우스를 올렸을 때 색
        public Color press;  // 누르고 있는 동안 색

        /// <summary>세 가지 색을 한 번에 지정해서 만든다.</summary>
        public ButtonColors(Color normal, Color hover, Color press)
        {
            this.normal = normal;
            this.hover = hover;
            this.press = press;
        }
    }

    /// <summary>
    /// 게임에서 쓰는 색상을 한 곳에 모아둔 파일. 색 조정은 여기서만 하면 모든 카드/버튼에 반영된다.
    /// Color(r, g, b)의 각 값은 0~1 범위다(255를 1로 본 비율).
    /// </summary>
    public static class GamePalette
    {
        // ---- 진영 색 (카드 테두리/네임플레이트) ----
        public static readonly Color Joseon = new Color(0.14f, 0.20f, 0.42f); // 조선: 짙은 남색
        public static readonly Color Qing = new Color(0.42f, 0.10f, 0.09f);   // 청: 짙은 적갈색
        public static readonly Color Ming = new Color(0.45f, 0.36f, 0.10f);   // 명: 황토색(확장용)

        // ---- 카드 종류 표시 글자 색 ----
        public static readonly Color KindWeapon = new Color(0.95f, 0.78f, 0.35f);    // 장비: 금색
        public static readonly Color KindSpell = new Color(0.55f, 0.85f, 1.00f);     // 전술: 하늘색
        public static readonly Color KindFormation = new Color(0.70f, 0.90f, 0.55f); // 진: 연두색

        /// <summary>카드 종류 표시 글자 색.</summary>
        public static Color KindColor(CardKind kind)
        {
            switch (kind)
            {
                case CardKind.Weapon:    return KindWeapon;
                case CardKind.Spell:     return KindSpell;
                case CardKind.Formation: return KindFormation;
                default:                 return Color.white;
            }
        }

        // ---- 덱 더미 ----
        public static readonly Color PileLabel = new Color(0.96f, 0.92f, 0.80f); // 덱 더미 아래 글자
        public static readonly Color PileHover = new Color(1.00f, 0.95f, 0.75f); // 마우스를 올렸을 때 카드 뒷면 색(살짝 밝게)

        /// <summary>진영에 맞는 색을 돌려준다.</summary>
        public static Color FactionColor(Faction faction)
        {
            switch (faction)
            {
                case Faction.Qing: return Qing;
                case Faction.Ming: return Ming;
                default:           return Joseon;
            }
        }

        // ---- 희귀도 색 (카드 우상단 리본) ----
        public static readonly Color RarityCommon = new Color(0.55f, 0.55f, 0.55f);    // 일반: 회색 (실제로는 리본을 숨김)
        public static readonly Color RarityElite = new Color(0.30f, 0.55f, 0.80f);     // 엘리트: 파랑
        public static readonly Color RarityLegendary = new Color(0.85f, 0.58f, 0.12f); // 전설: 주황/금색
        public static readonly Color RarityHero = new Color(0.62f, 0.14f, 0.62f);      // 히어로: 보라

        /// <summary>희귀도에 맞는 리본 색을 돌려준다.</summary>
        public static Color RarityColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Hero:      return RarityHero;
                case Rarity.Legendary: return RarityLegendary;
                case Rarity.Elite:     return RarityElite;
                default:               return RarityCommon;
            }
        }

        // ---- 카드 부품 색 ----
        public static readonly Color AbilityBoxBg = new Color(0.04f, 0.04f, 0.05f, 0.80f); // 능력 설명 상자 배경(반투명 검정)
        public static readonly Color AttackBadge = new Color(0.55f, 0.10f, 0.08f);         // 공격력 배지(빨강)
        public static readonly Color HealthBadge = new Color(0.10f, 0.28f, 0.55f);         // 체력 배지(파랑)
        public const float NamePlateAlpha = 0.92f;                                          // 네임플레이트 불투명도

        // ---- 버튼 스타일 (ClickableButton을 상속한 버튼이 이 중 하나를 고른다) ----
        // 금색: 턴 종료, 팩 열기처럼 눈에 띄어야 하는 버튼
        public static readonly ButtonColors GoldButton = new ButtonColors(
            new Color(0.55f, 0.42f, 0.12f), new Color(0.75f, 0.58f, 0.20f), new Color(0.35f, 0.26f, 0.07f)); // 평소, 마우스 올림, 누름
        // 회청색: 덱 편집, 닫기처럼 보조 역할의 버튼
        public static readonly ButtonColors NeutralButton = new ButtonColors(
            new Color(0.30f, 0.32f, 0.40f), new Color(0.42f, 0.45f, 0.56f), new Color(0.20f, 0.21f, 0.27f)); // 평소, 마우스 올림, 누름
        // 초록: 저장처럼 "확정" 의미의 버튼
        public static readonly ButtonColors GreenButton = new ButtonColors(
            new Color(0.14f, 0.42f, 0.20f), new Color(0.20f, 0.58f, 0.28f), new Color(0.09f, 0.28f, 0.13f)); // 평소, 마우스 올림, 누름

        // ---- 승패 배너 ----
        public static readonly Color ResultVictory = new Color(1f, 0.85f, 0.3f);      // "승리!" 금색
        public static readonly Color ResultDefeat = new Color(0.9f, 0.3f, 0.28f);     // "패배..." 빨강
        public static readonly Color ResultDraw = new Color(0.85f, 0.85f, 0.85f);     // "무승부" 회색
        public static readonly Color ResultBackdrop = new Color(0f, 0f, 0f, 0.78f);   // 배너 뒤 어두운 띠
        public static readonly Color ResultHint = new Color(1f, 1f, 1f, 0.9f);        // 배너 아래 안내 문구

        // ---- 안내 문구 색 ----
        public static readonly Color TextOk = new Color(0.55f, 0.85f, 0.45f);   // 조건 충족(연두)
        public static readonly Color TextWarn = new Color(0.85f, 0.45f, 0.35f); // 조건 미달(주황빛 빨강)
    }
}
