using UnityEngine;
using System.Text;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// CardData 한 장을 실제 하스스톤/섀도우버스 스타일 카드로 그려주는 컴포넌트.
    /// 프레임/초상화/코스트/희귀도 리본/능력 박스/네임플레이트/공격력·체력 배지를 전부 조립해서
    /// CardData의 값으로 채워 넣는다(Setup 한 번 호출로 전체 표시가 완성됨).
    /// 손패에서 카드끼리 겹칠 때 z-fighting이 나지 않도록 SetLayerBase로 카드 단위 정렬 순서도 관리한다.
    /// </summary>
    public class CardView : MonoBehaviour
    {
        public CardData data;

        // 필드에서의 실시간 체력. data.health는 카드의 "최대치" 설계값이고, 이 값은 전투 중 실제로 깎여나간다.
        // Setup()에서 매번 data.health로 초기화되므로 같은 CardData를 참조하는 여러 인스턴스끼리 서로 침범하지 않는다.
        [System.NonSerialized] public int currentHealth;
        public bool IsDead { get { return currentHealth <= 0; } }

        // ---- 프리팹 자식 오브젝트 참조 (전부 CardView.prefab에서 인스펙터로 연결됨) ----
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

        // ---- 진영/희귀도별 색상표 ----
        static readonly Color JoseonColor = new Color(0.14f, 0.20f, 0.42f);
        static readonly Color QingColor = new Color(0.42f, 0.10f, 0.09f);

        static readonly Color RarityCommon = new Color(0.55f, 0.55f, 0.55f);
        static readonly Color RarityElite = new Color(0.30f, 0.55f, 0.80f);
        static readonly Color RarityLegendary = new Color(0.85f, 0.58f, 0.12f);
        static readonly Color RarityHero = new Color(0.62f, 0.14f, 0.62f);

        const int AbilityWrapChars = 10; // 능력 설명 줄바꿈 기준 글자수(한글 기준, 능력 박스 실측 폭에 맞춰 보정됨)

        // 영문 카드명이 카드 폭을 넘어 우하단 공격력/체력 배지를 침범하지 않도록 글자 수에 반비례해
        // characterSize를 계산하는 보정 상수들. width(단위: 월드 유닛) ~= NameWidthK * 글자수 * characterSize.
        const float NameWidthK = 1.6f;
        const float NameCharSizeMax = 0.042f;
        const float NameCharSizeMin = 0.018f;
        const float NameTargetWidth = 0.85f;

        // 초상화 박스의 목표 크기(월드 유닛). 소스 초상화 텍스처마다 파이프라인이 달라 해상도/PPU가
        // 제각각이므로, 고정 localScale은 우연히만 맞는다. 대신 Setup()에서 매 카드마다 스프라이트의
        // 실제 로컬 바운드(원본 크기)를 읽어 항상 이 크기로 늘어나도록 스케일을 역산한다.
        static readonly Vector2 PortraitTargetSize = new Vector2(1.15f, 1.55f);

        List<Renderer> layerRenderers;
        List<int> layerRelativeOrders;

        /// <summary>이 카드를 구성하는 모든 Renderer와, 서로 간의 상대적 그리기 순서(원래 sortingOrder)를 최초 1회 캐싱한다.</summary>
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

        /// <summary>
        /// 카드 전체에 고유한 sortingOrder "버킷"을 부여해서, 손패에서 카드끼리 겹칠 때
        /// 이 카드의 프레임/초상화/텍스트가 절대 다른 카드의 레이어와 뒤섞이지 않게 한다.
        /// (baseIndex가 클수록 위에 그려짐. 드래그 중인 카드는 CardDragHandler가 아주 큰 값을 넘겨 항상 맨 위로 올린다.)
        /// </summary>
        public void SetLayerBase(int baseIndex)
        {
            CaptureLayerOrders();
            for (int i = 0; i < layerRenderers.Count; i++)
            {
                if (layerRenderers[i] != null)
                    layerRenderers[i].sortingOrder = baseIndex * 20 + layerRelativeOrders[i];
            }
        }

        /// <summary>CardData의 내용을 이 카드의 모든 시각 요소(프레임 색상/초상화/텍스트/배지 등)에 반영한다.</summary>
        public void Setup(CardData d)
        {
            data = d;
            if (d == null) return;

            currentHealth = d.health;

            bool isJoseon = d.faction == Faction.Joseon;
            Color factionColor = isJoseon ? JoseonColor : QingColor;

            if (frameRenderer != null) frameRenderer.color = factionColor;
            if (namePlateRenderer != null) namePlateRenderer.color = new Color(factionColor.r, factionColor.g, factionColor.b, 0.92f);
            if (abilityBgRenderer != null) abilityBgRenderer.color = new Color(0.04f, 0.04f, 0.05f, 0.80f);

            if (portraitRenderer != null && d.portrait != null)
            {
                portraitRenderer.sprite = d.portrait;
                // 이 스프라이트만의 원본(PPU 반영) 로컬 바운드를 읽어서, 텍스처 임포트 설정과 무관하게
                // 항상 PortraitTargetSize로 렌더링되도록 스케일을 역산한다.
                Vector2 native = d.portrait.bounds.size;
                if (native.x > 0.0001f && native.y > 0.0001f)
                {
                    portraitRenderer.transform.localScale = new Vector3(
                        PortraitTargetSize.x / native.x,
                        PortraitTargetSize.y / native.y,
                        1f);
                }
            }

            if (nameText != null)
            {
                nameText.text = d.cardNameEn;
                int len = string.IsNullOrEmpty(d.cardNameEn) ? 1 : d.cardNameEn.Length;
                float cs = NameTargetWidth / (NameWidthK * len);
                nameText.characterSize = Mathf.Clamp(cs, NameCharSizeMin, NameCharSizeMax);
            }
            if (nameTextKo != null) nameTextKo.text = d.cardNameKo;

            if (costText != null) costText.text = d.cost.ToString();
            if (atkText != null) atkText.text = d.attack.ToString();
            if (hpText != null) hpText.text = d.health.ToString();

            string abilityName, abilityDesc;
            ParseKeyword(d.keywordText, out abilityName, out abilityDesc);

            bool hasAbility = !string.IsNullOrEmpty(abilityName);
            if (abilityBoxRoot != null) abilityBoxRoot.SetActive(hasAbility);
            if (abilityNameText != null) abilityNameText.text = WrapText(abilityName, AbilityWrapChars);
            if (abilityDescText != null) abilityDescText.text = WrapText(abilityDesc, AbilityWrapChars);

            if (flavorTextMesh != null) flavorTextMesh.text = d.flavorText ?? "";

            Color rarityColor; string rarityLabel;
            GetRarityStyle(d.rarity, out rarityColor, out rarityLabel);
            bool showRibbon = d.rarity != Rarity.Common; // Common은 리본을 아예 숨김(일반 카드는 등급 표시가 없는 편이 자연스러움)
            if (rarityRibbonRoot != null) rarityRibbonRoot.SetActive(showRibbon);
            if (rarityRibbonRenderer != null) rarityRibbonRenderer.color = rarityColor;
            if (rarityText != null) rarityText.text = rarityLabel;

            if (atkBadgeRenderer != null) atkBadgeRenderer.color = new Color(0.55f, 0.10f, 0.08f);
            if (hpBadgeRenderer != null) hpBadgeRenderer.color = new Color(0.10f, 0.28f, 0.55f);
        }

        /// <summary>전투에서 이 카드가 받는 피해를 currentHealth에 반영하고 체력 배지 텍스트를 갱신한다. 사망 여부는 IsDead로 별도 확인.</summary>
        public void ApplyDamage(int amount)
        {
            if (amount <= 0) return;
            currentHealth -= amount;
            if (hpText != null) hpText.text = currentHealth.ToString();
        }

        /// <summary>"키워드명: 설명" 형식의 문자열을 콜론(:) 기준으로 이름/설명으로 분리한다. 콜론이 없으면 전부 이름으로 취급.</summary>
        static void ParseKeyword(string keywordText, out string name, out string desc)
        {
            name = "";
            desc = "";
            if (string.IsNullOrEmpty(keywordText)) return;
            int idx = keywordText.IndexOf(':');
            if (idx >= 0)
            {
                name = keywordText.Substring(0, idx).Trim();
                desc = keywordText.Substring(idx + 1).Trim();
            }
            else
            {
                name = keywordText.Trim();
                desc = "";
            }
        }

        /// <summary>희귀도별로 리본 색상과 표시 문자열(HERO/LEGENDARY/ELITE)을 정한다. Common은 빈 문자열(리본 자체가 숨겨지므로 실제로 안 보임).</summary>
        static void GetRarityStyle(Rarity r, out Color color, out string label)
        {
            switch (r)
            {
                case Rarity.Hero: color = RarityHero; label = "HERO"; break;
                case Rarity.Legendary: color = RarityLegendary; label = "LEGENDARY"; break;
                case Rarity.Elite: color = RarityElite; label = "ELITE"; break;
                default: color = RarityCommon; label = ""; break;
            }
        }

        /// <summary>공백 기준 단어 단위로 줄바꿈하는 간단한 워드랩. TextMesh는 자동 줄바꿈을 지원하지 않아 직접 구현했다.</summary>
        static string WrapText(string s, int maxCharsPerLine)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string[] words = s.Split(' ');
            var sb = new StringBuilder();
            int lineLen = 0;
            foreach (var w in words)
            {
                if (lineLen > 0 && lineLen + 1 + w.Length > maxCharsPerLine)
                {
                    sb.Append('\n');
                    lineLen = 0;
                }
                else if (lineLen > 0)
                {
                    sb.Append(' ');
                    lineLen += 1;
                }
                sb.Append(w);
                lineLen += w.Length;
            }
            return sb.ToString();
        }
    }
}
