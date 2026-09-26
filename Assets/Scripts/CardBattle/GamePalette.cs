using UnityEngine;

namespace CardBattle
{
    /// <summary>버튼 하나의 상태별 배경색(평소 / 마우스 올림 / 누르는 중).</summary>
    public struct ButtonColors
    {
        public Color normal;
        public Color hover;
        public Color press;

        public ButtonColors(Color normal, Color hover, Color press)
        {
            this.normal = normal;
            this.hover = hover;
            this.press = press;
        }
    }

    /// <summary>
    /// 게임에서 쓰는 색상을 한 곳에 모아둔 파일. 색 조정은 여기서만 하면 모든 카드/버튼에 반영된다.
    /// </summary>
    public static class GamePalette
    {
        // ---- 진영 ----
        public static readonly Color Joseon = new Color(0.14f, 0.20f, 0.42f);
        public static readonly Color Qing = new Color(0.42f, 0.10f, 0.09f);
        public static readonly Color Ming = new Color(0.45f, 0.36f, 0.10f);

        public static Color FactionColor(Faction faction)
        {
            switch (faction)
            {
                case Faction.Qing: return Qing;
                case Faction.Ming: return Ming;
                default:           return Joseon;
            }
        }

        // ---- 희귀도 (리본 색) ----
        public static readonly Color RarityCommon = new Color(0.55f, 0.55f, 0.55f);
        public static readonly Color RarityElite = new Color(0.30f, 0.55f, 0.80f);
        public static readonly Color RarityLegendary = new Color(0.85f, 0.58f, 0.12f);
        public static readonly Color RarityHero = new Color(0.62f, 0.14f, 0.62f);

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

        // ---- 카드 부품 ----
        public static readonly Color AbilityBoxBg = new Color(0.04f, 0.04f, 0.05f, 0.80f);
        public static readonly Color AttackBadge = new Color(0.55f, 0.10f, 0.08f);
        public static readonly Color HealthBadge = new Color(0.10f, 0.28f, 0.55f);
        public const float NamePlateAlpha = 0.92f;

        // ---- 버튼 스타일 (ClickableButton 하위 클래스가 이 중 하나를 고른다) ----
        public static readonly ButtonColors GoldButton = new ButtonColors(
            new Color(0.55f, 0.42f, 0.12f), new Color(0.75f, 0.58f, 0.20f), new Color(0.35f, 0.26f, 0.07f));
        public static readonly ButtonColors NeutralButton = new ButtonColors(
            new Color(0.30f, 0.32f, 0.40f), new Color(0.42f, 0.45f, 0.56f), new Color(0.20f, 0.21f, 0.27f));
        public static readonly ButtonColors GreenButton = new ButtonColors(
            new Color(0.14f, 0.42f, 0.20f), new Color(0.20f, 0.58f, 0.28f), new Color(0.09f, 0.28f, 0.13f));

        // ---- 안내 문구 ----
        public static readonly Color TextOk = new Color(0.55f, 0.85f, 0.45f);
        public static readonly Color TextWarn = new Color(0.85f, 0.45f, 0.35f);
    }
}
