using UnityEngine;
using System.Text;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// CardData 한 장을 하스스톤/섀도우버스 스타일 카드로 그려주는 컴포넌트(CardView.prefab 루트에 붙어있음).
    /// Setup(data) 한 번 호출로 프레임/초상화/코스트/희귀도 리본/능력 박스/네임플레이트/공격력·체력 배지가 전부 채워진다.
    /// 필드에서 전투 중 깎이는 실제 체력(currentHealth)도 여기서 관리한다.
    ///
    /// 색상은 GamePalette.cs, 키워드 해석은 CardData.KeywordName/KeywordDescription에서 가져온다.
    /// 이 파일에는 "카드 모양을 그리는 방법"만 남겨두었다.
    /// </summary>
    public class CardView : MonoBehaviour
    {
        public CardData data;

        /// <summary>필드에서의 실시간 체력. data.health는 설계상 최대치이고, 이 값이 전투 중 실제로 깎인다.</summary>
        [System.NonSerialized] public int currentHealth;
        public bool IsDead { get { return currentHealth <= 0; } }

        // ---- 프리팹 자식 오브젝트 참조 (CardView.prefab에서 인스펙터로 연결됨 — 이름을 바꾸면 연결이 끊기니 주의) ----
        public SpriteRenderer frameRenderer;
        public SpriteRenderer portraitRenderer;
        public SpriteRenderer namePlateRenderer;
        public SpriteRenderer abilityBgRenderer;
        public SpriteRenderer costBadgeRenderer;
        public SpriteRenderer rarityRibbonRenderer;
        public SpriteRenderer atkBadgeRenderer;
        public SpriteRenderer hpBadgeRenderer;

        public TextMesh nameText;
        public TextMesh nameTextKo;
        public TextMesh costText;
        public TextMesh atkText;
        public TextMesh hpText;
        public TextMesh rarityText;
        public TextMesh abilityNameText;
        public TextMesh abilityDescText;
        public TextMesh flavorTextMesh;

        public GameObject rarityRibbonRoot;
        public GameObject abilityBoxRoot;

        // ---- 레이아웃 보정 상수 ----
        const int AbilityWrapChars = 10; // 능력 설명 줄바꿈 기준 글자수(능력 박스 실측 폭에 맞춘 값)

        // 영문 카드명이 카드 폭을 넘어 공격력/체력 배지를 가리지 않도록, 글자 수에 반비례해 글자 크기를 줄인다.
        // 대략 폭(월드 유닛) ≈ NameWidthK × 글자수 × characterSize
        const float NameWidthK = 1.6f;
        const float NameCharSizeMax = 0.042f;
        const float NameCharSizeMin = 0.018f;
        const float NameTargetWidth = 0.85f;

        // 초상화 목표 크기(월드 유닛). 원본 텍스처마다 해상도/PPU가 달라서, 매번 원본 크기를 읽어 이 크기로 맞춘다.
        static readonly Vector2 PortraitTargetSize = new Vector2(1.15f, 1.55f);

        // 카드 하나 안의 렌더러들이 서로 겹치는 순서를 유지하면서 카드 단위로 앞/뒤를 바꾸기 위한 간격.
        public const int SortingOrdersPerCard = 20;

        List<Renderer> layerRenderers;
        List<int> layerRelativeOrders;

        /// <summary>CardData의 내용을 이 카드의 모든 시각 요소에 반영하고, 실시간 체력을 최대치로 초기화한다.</summary>
        public void Setup(CardData d)
        {
            data = d;
            if (d == null) return;

            currentHealth = d.health;

            ApplyFrameColors(d);
            ApplyPortrait(d);
            ApplyName(d);
            ApplyStats(d);
            ApplyAbility(d);
            ApplyRarity(d);
            UnityUtil.SetText(flavorTextMesh, d.flavorText ?? "");
        }

        /// <summary>전투 피해를 currentHealth에 반영하고 체력 배지를 갱신한다. 사망 여부는 IsDead로 확인.</summary>
        public void ApplyDamage(int amount)
        {
            if (amount <= 0) return;
            currentHealth -= amount;
            UnityUtil.SetText(hpText, Mathf.Max(0, currentHealth).ToString());
        }

        /// <summary>
        /// 카드 전체에 고유한 sortingOrder "버킷"을 준다. baseIndex가 클수록 위에 그려지고,
        /// 카드 안의 부품(프레임/초상화/글자) 사이 순서는 그대로 유지된다.
        /// </summary>
        public void SetLayerBase(int baseIndex)
        {
            CaptureLayerOrders();
            for (int i = 0; i < layerRenderers.Count; i++)
            {
                if (layerRenderers[i] != null)
                    layerRenderers[i].sortingOrder = baseIndex * SortingOrdersPerCard + layerRelativeOrders[i];
            }
        }

        // ------------------------------------------------------------------
        // Setup 세부 단계
        // ------------------------------------------------------------------

        void ApplyFrameColors(CardData d)
        {
            Color factionColor = GamePalette.FactionColor(d.faction);
            if (frameRenderer != null) frameRenderer.color = factionColor;
            if (namePlateRenderer != null)
                namePlateRenderer.color = new Color(factionColor.r, factionColor.g, factionColor.b, GamePalette.NamePlateAlpha);
            if (abilityBgRenderer != null) abilityBgRenderer.color = GamePalette.AbilityBoxBg;
            if (atkBadgeRenderer != null) atkBadgeRenderer.color = GamePalette.AttackBadge;
            if (hpBadgeRenderer != null) hpBadgeRenderer.color = GamePalette.HealthBadge;
        }

        void ApplyPortrait(CardData d)
        {
            if (portraitRenderer == null || d.portrait == null) return;
            portraitRenderer.sprite = d.portrait;

            Vector2 native = d.portrait.bounds.size; // PPU가 반영된 원본 크기
            if (native.x > 0.0001f && native.y > 0.0001f)
            {
                portraitRenderer.transform.localScale = new Vector3(
                    PortraitTargetSize.x / native.x,
                    PortraitTargetSize.y / native.y,
                    1f);
            }
        }

        void ApplyName(CardData d)
        {
            if (nameText != null)
            {
                nameText.text = d.cardNameEn;
                int len = string.IsNullOrEmpty(d.cardNameEn) ? 1 : d.cardNameEn.Length;
                float size = NameTargetWidth / (NameWidthK * len);
                nameText.characterSize = Mathf.Clamp(size, NameCharSizeMin, NameCharSizeMax);
            }
            UnityUtil.SetText(nameTextKo, d.cardNameKo);
        }

        void ApplyStats(CardData d)
        {
            UnityUtil.SetText(costText, d.cost.ToString());
            UnityUtil.SetText(atkText, d.attack.ToString());
            UnityUtil.SetText(hpText, d.health.ToString());
        }

        void ApplyAbility(CardData d)
        {
            string abilityName = d.KeywordName;
            bool hasAbility = !string.IsNullOrEmpty(abilityName);
            if (abilityBoxRoot != null) abilityBoxRoot.SetActive(hasAbility);
            UnityUtil.SetText(abilityNameText, WrapText(abilityName, AbilityWrapChars));
            UnityUtil.SetText(abilityDescText, WrapText(d.KeywordDescription, AbilityWrapChars));
        }

        void ApplyRarity(CardData d)
        {
            bool showRibbon = d.rarity != Rarity.Common; // 일반 카드는 등급 리본을 숨긴다
            if (rarityRibbonRoot != null) rarityRibbonRoot.SetActive(showRibbon);
            if (rarityRibbonRenderer != null) rarityRibbonRenderer.color = GamePalette.RarityColor(d.rarity);
            UnityUtil.SetText(rarityText, RarityLabel(d.rarity));
        }

        static string RarityLabel(Rarity r)
        {
            switch (r)
            {
                case Rarity.Hero:      return "HERO";
                case Rarity.Legendary: return "LEGENDARY";
                case Rarity.Elite:     return "ELITE";
                default:               return "";
            }
        }

        /// <summary>카드를 구성하는 모든 Renderer와 원래 sortingOrder를 최초 1회 기억해둔다.</summary>
        void CaptureLayerOrders()
        {
            if (layerRenderers != null) return;
            layerRenderers = new List<Renderer>();
            layerRelativeOrders = new List<int>();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                layerRenderers.Add(r);
                layerRelativeOrders.Add(r.sortingOrder);
            }
        }

        /// <summary>공백 기준 단어 단위 줄바꿈. TextMesh는 자동 줄바꿈이 없어서 직접 처리한다.</summary>
        static string WrapText(string s, int maxCharsPerLine)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder();
            int lineLen = 0;
            foreach (var word in s.Split(' '))
            {
                if (lineLen > 0 && lineLen + 1 + word.Length > maxCharsPerLine)
                {
                    sb.Append('\n');
                    lineLen = 0;
                }
                else if (lineLen > 0)
                {
                    sb.Append(' ');
                    lineLen += 1;
                }
                sb.Append(word);
                lineLen += word.Length;
            }
            return sb.ToString();
        }
    }
}
