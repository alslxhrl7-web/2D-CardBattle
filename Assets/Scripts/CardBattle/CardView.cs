using UnityEngine;
using System.Text;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// CardData 한 장을 하스스톤/섀도우버스 스타일 카드로 그려주는 컴포넌트(CardView.prefab 루트에 붙어있음).
    /// Setup(data) 한 번 호출로 테두리/초상화/코스트/희귀도 리본/능력 상자/네임플레이트/공격력·체력 배지가 전부 채워진다.
    /// 필드에서 전투 중 깎이는 실제 체력(currentHealth)도 여기서 관리한다.
    ///
    /// 색상은 GamePalette.cs, 키워드 해석은 CardData.KeywordName/KeywordDescription에서 가져온다.
    /// 이 파일에는 "카드 모양을 그리는 방법"만 남겨두었다.
    /// </summary>
    public class CardView : MonoBehaviour
    {
        public CardData data; // 이 카드가 보여주고 있는 카드 데이터

        /// <summary>필드에서의 실시간 체력. data.health는 설계상 최대치이고, 이 값이 전투 중 실제로 깎인다.</summary>
        [System.NonSerialized] public int currentHealth;

        /// <summary>필드에서의 실시간 공격력. 처음엔 data.attack이고, 장비·전술 효과로 오를 수 있다.</summary>
        [System.NonSerialized] public int currentAttack;

        bool lastStandUsed; // Last Stand 효과가 이미 발동했는지 (한 번만)

        /// <summary>체력이 0 이하가 되어 죽었는지.</summary>
        public bool IsDead { get { return currentHealth <= 0; } }

        // ---- 프리팹 자식 오브젝트 참조 (CardView.prefab에서 인스펙터로 연결됨 — 이름을 바꾸면 연결이 끊기니 주의) ----
        public SpriteRenderer frameRenderer;        // 카드 테두리(진영색)
        public SpriteRenderer portraitRenderer;     // 초상화 그림
        public SpriteRenderer namePlateRenderer;    // 이름이 적히는 띠
        public SpriteRenderer abilityBgRenderer;    // 능력 설명 상자 배경
        public SpriteRenderer rarityRibbonRenderer; // 우상단 희귀도 리본
        public SpriteRenderer atkBadgeRenderer;     // 공격력 배지
        public SpriteRenderer hpBadgeRenderer;      // 체력 배지

        public TextMesh nameText;        // 영문 이름
        public TextMesh nameTextKo;      // 한글 이름
        public TextMesh costText;        // 코스트 숫자
        public TextMesh atkText;         // 공격력 숫자
        public TextMesh hpText;          // 체력 숫자 (전투 중에는 현재 체력이 표시됨)
        public TextMesh rarityText;      // 희귀도 글자 (HERO/LEGENDARY/ELITE)
        public TextMesh abilityNameText; // 키워드 이름
        public TextMesh abilityDescText; // 키워드 설명
        public TextMesh flavorTextMesh;  // 맨 아래 설정 문구

        public GameObject rarityRibbonRoot; // 희귀도 리본 전체 (일반 카드는 숨김)
        public GameObject abilityBoxRoot;   // 능력 상자 전체 (키워드가 없으면 숨김)

        // ---- 글자 크기 (카드 위에서의 글자 높이, 월드 유닛) — 글자가 크거나 작으면 여기 숫자만 바꾸면 된다 ----
        // 카드 크기는 가로 1.35 × 세로 2.05 이다.
        const float CostTextHeight = 0.26f;      // 좌상단 코스트 숫자
        const float StatTextHeight = 0.15f;      // 공격력/체력 숫자
        const float NameKoHeight = 0.105f;       // 한글 이름
        const float NameEnHeightMax = 0.10f;     // 영문 이름 (짧은 이름일 때 최대)
        const float NameEnHeightMin = 0.055f;    // 영문 이름 (긴 이름일 때 최소)
        const float AbilityNameHeight = 0.085f;  // 키워드 이름
        const float AbilityDescHeight = 0.072f;  // 키워드 설명 (작아서 안 보이면 이 값을 올린다. 카드 위에 마우스를 올리면 크게도 보임)
        const float RarityTextHeight = 0.055f;   // 희귀도 리본 글자
        const float FlavorTextHeight = 0.045f;   // 맨 아래 설정 문구
        const float KindLabelHeight = 0.075f;    // 초상화 위쪽 가운데의 카드 종류 표시 ("장비", "전술", "진")

        // 카드 종류 표시 위치 (카드 중심 기준, 카드 크기 1.35 × 2.05). 코스트 원과 희귀도 리본 사이 윗부분.
        static readonly Vector3 KindLabelLocalPos = new Vector3(0f, 0.9f, -0.01f);
        const string KindLabelName = "KindLabel"; // 실행 중에 만들어지는 종류 표시 오브젝트 이름

        const int AbilityWrapChars = 10;          // 능력 설명 줄바꿈 기준 글자수(능력 상자 실제 폭에 맞춘 값)

        // 영문 카드명이 카드 폭을 넘어 공격력/체력 배지를 가리지 않도록, 글자 수가 많으면 글자를 줄인다.
        // 영문 한 글자 폭 ≈ 글자 높이 × NameCharWidthRatio 로 보고 계산한다.
        const float NameCharWidthRatio = 0.5f;   // 영문 글자 폭 / 높이 비율(대략값)
        const float NameTargetWidth = 0.85f;     // 영문 이름이 차지해도 되는 최대 폭

        // 초상화 목표 크기(월드 유닛). 원본 그림마다 해상도가 달라서, 매번 원본 크기를 읽어 이 크기로 맞춘다.
        static readonly Vector2 PortraitTargetSize = new Vector2(1.15f, 1.55f);

        // 카드 하나 안의 부품끼리 겹치는 순서를 유지하면서 카드 단위로 앞/뒤를 바꾸기 위한 간격.
        public const int SortingOrdersPerCard = 20;

        List<Renderer> layerRenderers;   // 이 카드의 모든 렌더러 (처음 한 번 수집)
        List<int> layerRelativeOrders;   // 각 렌더러의 원래 그리기 순서
        int layerBase;                   // 마지막으로 받은 정렬 구간 (SetLayerBase)

        SpriteRenderer backCover;        // 뒷면 덮개. 2인 대전에서 차례가 아닌 쪽 손패를 가린다 (필요할 때 만든다)
        static readonly Vector2 CardSize = new Vector2(1.35f, 2.05f); // 카드 한 장 크기 (월드 유닛)

        /// <summary>지금 뒷면으로 덮여 있는지.</summary>
        public bool IsFaceDown { get { return backCover != null && backCover.gameObject.activeSelf; } }

        /// <summary>CardData의 내용을 이 카드의 모든 표시 요소에 반영하고, 실시간 체력을 최대치로 초기화한다.</summary>
        public void Setup(CardData d)
        {
            data = d;
            if (d == null) return;

            currentHealth = d.health; // 전투용 체력은 최대치에서 시작
            currentAttack = d.attack; // 전투용 공격력도 카드에 적힌 값에서 시작
            lastStandUsed = false;

            ApplyTextSizes();
            ApplyFrameColors(d);
            ApplyPortrait(d);
            ApplyName(d);
            ApplyStats(d);
            ApplyAbility(d);
            ApplyRarity(d);
            ApplyKindLabel(d);
            UnityUtil.SetText(flavorTextMesh, d.flavorText ?? "");
        }

        /// <summary>전투 피해를 currentHealth에 반영하고 체력 배지를 갱신한다. 죽었는지는 IsDead로 확인.</summary>
        public void ApplyDamage(int amount)
        {
            if (amount <= 0) return;
            currentHealth -= amount;
            // Last Stand: 살아남았는데 체력이 기준 미만이 되면 공격력이 한 번 오른다
            if (!IsDead && !lastStandUsed && currentHealth < CardKeywords.LastStandHealth && data != null && data.HasKeyword(CardKeywords.LastStand))
            {
                lastStandUsed = true;
                currentAttack += CardKeywords.LastStandBonus;
            }
            RefreshStatTexts(); // 화면에는 0 아래로 표시하지 않음
        }

        /// <summary>손패에서 실제로 내야 하는 비용(Swarm 할인 반영)을 코스트 숫자에 적는다.</summary>
        public void ShowCost(int cost)
        {
            UnityUtil.SetText(costText, cost.ToString());
        }

        /// <summary>장비·전술 효과로 공격력/체력을 올린다(음수면 내린다). 숫자 배지도 바로 갱신한다.</summary>
        public void ApplyBuff(int attackBonus, int healthBonus)
        {
            currentAttack = Mathf.Max(0, currentAttack + attackBonus);
            currentHealth += healthBonus;
            RefreshStatTexts();
        }

        /// <summary>공격력/체력 배지에 지금 값(currentAttack/currentHealth)을 적는다.</summary>
        public void RefreshStatTexts()
        {
            UnityUtil.SetText(atkText, currentAttack.ToString());
            UnityUtil.SetText(hpText, Mathf.Max(0, currentHealth).ToString());
        }

        /// <summary>
        /// 카드 전체에 고유한 그리기 순서(sortingOrder) "구간"을 준다. baseIndex가 클수록 위에 그려지고,
        /// 카드 안의 부품(테두리/초상화/글자) 사이 순서는 그대로 유지된다.
        /// </summary>
        public void SetLayerBase(int baseIndex)
        {
            layerBase = baseIndex;
            CaptureLayerOrders();
            for (int i = 0; i < layerRenderers.Count; i++)
            {
                if (layerRenderers[i] != null)
                    layerRenderers[i].sortingOrder = baseIndex * SortingOrdersPerCard + layerRelativeOrders[i];
            }
        }

        // ================= Setup 세부 단계 =================

        /// <summary>모든 글자의 크기/굵기를 위의 "글자 크기" 상수대로 맞춘다(영문 이름은 ApplyName에서 따로 맞춤).</summary>
        void ApplyTextSizes()
        {
            UnityUtil.SetTextHeight(costText, CostTextHeight, true);
            UnityUtil.SetTextHeight(atkText, StatTextHeight, true);
            UnityUtil.SetTextHeight(hpText, StatTextHeight, true);
            UnityUtil.SetTextHeight(nameTextKo, NameKoHeight, true);
            UnityUtil.SetTextHeight(abilityNameText, AbilityNameHeight, true);
            UnityUtil.SetTextHeight(abilityDescText, AbilityDescHeight, false);
            UnityUtil.SetTextHeight(rarityText, RarityTextHeight, true);
            UnityUtil.SetTextHeight(flavorTextMesh, FlavorTextHeight, false);
        }

        /// <summary>테두리/네임플레이트/능력 상자/배지 색을 칠한다.</summary>
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

        /// <summary>초상화를 넣고, 원본 크기와 상관없이 항상 같은 크기로 보이도록 배율을 계산한다.</summary>
        void ApplyPortrait(CardData d)
        {
            if (portraitRenderer == null || d.portrait == null) return;
            portraitRenderer.sprite = d.portrait;

            Vector2 native = d.portrait.bounds.size; // 그림의 원본 크기(월드 유닛)
            if (native.x > 0.0001f && native.y > 0.0001f)
            {
                portraitRenderer.transform.localScale = new Vector3(
                    PortraitTargetSize.x / native.x,
                    PortraitTargetSize.y / native.y,
                    1f);
            }
        }

        /// <summary>이름을 넣고, 영문 이름 길이에 맞춰 글자 크기를 정한다(긴 이름은 작게).</summary>
        void ApplyName(CardData d)
        {
            if (nameText != null)
            {
                nameText.text = d.cardNameEn;
                int len = string.IsNullOrEmpty(d.cardNameEn) ? 1 : d.cardNameEn.Length;
                float height = NameTargetWidth / (NameCharWidthRatio * len); // 이름이 길수록 작아진다
                UnityUtil.SetTextHeight(nameText, Mathf.Clamp(height, NameEnHeightMin, NameEnHeightMax), false);
            }
            UnityUtil.SetText(nameTextKo, d.cardNameKo);
        }

        /// <summary>
        /// 코스트/공격력/체력 숫자를 넣는다. 카드 종류에 따라 배지 모양이 다르다.
        ///   유닛: 공격력/체력 그대로   장비: "+2", "+1"처럼 더해 줄 값   전술: 배지 숨김   진: 공격력 0이면 공격력 배지 숨김
        /// </summary>
        void ApplyStats(CardData d)
        {
            UnityUtil.SetText(costText, d.cost.ToString());

            bool showAttack = true, showHealth = true;
            if (d.IsSpell) { showAttack = false; showHealth = false; }
            else if (d.cardKind == CardKind.Formation && d.attack <= 0) showAttack = false;

            SetBadgeVisible(atkBadgeRenderer, atkText, showAttack);
            SetBadgeVisible(hpBadgeRenderer, hpText, showHealth);

            if (d.IsEquipment)
            {
                UnityUtil.SetText(atkText, "+" + d.attack);
                UnityUtil.SetText(hpText, "+" + d.health);
            }
            else
            {
                UnityUtil.SetText(atkText, d.attack.ToString());
                UnityUtil.SetText(hpText, d.health.ToString());
            }
        }

        /// <summary>배지(배경)와 숫자를 함께 보이거나 숨긴다.</summary>
        static void SetBadgeVisible(SpriteRenderer badge, TextMesh text, bool visible)
        {
            if (badge != null) badge.gameObject.SetActive(visible);
            if (text != null) text.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 초상화 위쪽 가운데에 카드 종류("장비", "전술", "진")를 적는다. 유닛은 표시하지 않는다.
        /// 표시용 글자는 한글 이름 글자를 복제해서 처음 한 번 만들고 이후 재사용한다.
        /// </summary>
        void ApplyKindLabel(CardData d)
        {
            string label = GameTexts.KindLabel(d.cardKind);
            var existing = transform.Find(KindLabelName);
            TextMesh kindText = existing != null ? existing.GetComponent<TextMesh>() : null;

            if (string.IsNullOrEmpty(label))
            {
                if (existing != null) existing.gameObject.SetActive(false);
                return;
            }
            if (kindText == null)
            {
                if (nameTextKo == null) return; // 복제할 글자 스타일이 없음
                var go = Object.Instantiate(nameTextKo.gameObject, transform);
                go.name = KindLabelName;
                go.transform.localPosition = KindLabelLocalPos;
                go.transform.localRotation = Quaternion.identity;
                kindText = go.GetComponent<TextMesh>();
                kindText.anchor = TextAnchor.MiddleCenter;
                kindText.alignment = TextAlignment.Center;
            }
            kindText.gameObject.SetActive(true);
            UnityUtil.SetTextHeight(kindText, KindLabelHeight, true);
            kindText.text = label;
            kindText.color = GamePalette.KindColor(d.cardKind);
        }

        /// <summary>능력 상자에 키워드 이름/설명을 넣는다. 키워드가 없으면 상자를 숨긴다.</summary>
        void ApplyAbility(CardData d)
        {
            string abilityName = d.KeywordName;
            bool hasAbility = !string.IsNullOrEmpty(abilityName);
            if (abilityBoxRoot != null) abilityBoxRoot.SetActive(hasAbility);
            UnityUtil.SetText(abilityNameText, WrapText(abilityName, AbilityWrapChars));
            UnityUtil.SetText(abilityDescText, WrapText(d.KeywordDescription, AbilityWrapChars));
        }

        /// <summary>희귀도 리본의 색/글자를 정한다. 일반 카드는 리본을 숨긴다.</summary>
        void ApplyRarity(CardData d)
        {
            bool showRibbon = d.rarity != Rarity.Common; // 일반 카드는 등급 리본을 숨긴다
            if (rarityRibbonRoot != null) rarityRibbonRoot.SetActive(showRibbon);
            if (rarityRibbonRenderer != null) rarityRibbonRenderer.color = GamePalette.RarityColor(d.rarity);
            UnityUtil.SetText(rarityText, RarityLabel(d.rarity));
        }

        /// <summary>희귀도 리본에 적을 글자.</summary>
        static string RarityLabel(Rarity r)
        {
            switch (r)
            {
                case Rarity.Hero:      return "HERO";
                case Rarity.Legendary: return "LEGENDARY";
                case Rarity.Elite:     return "ELITE";
                default:               return ""; // 일반 카드는 리본 자체를 숨김
            }
        }

        /// <summary>카드를 뒷면으로 덮거나(true) 다시 앞면을 보인다(false).</summary>
        public void SetFaceDown(bool faceDown)
        {
            if (backCover == null)
            {
                if (!faceDown) return; // 덮은 적이 없으면 할 일 없음
                backCover = CreateBackCover();
            }
            backCover.gameObject.SetActive(faceDown);
        }

        /// <summary>카드 크기의 뒷면 그림을 카드 맨 위에 만든다. (덱 더미와 같은 그림)</summary>
        SpriteRenderer CreateBackCover()
        {
            var go = new GameObject("BackCover");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, -0.05f);

            var sr = go.AddComponent<SpriteRenderer>();
            var back = Resources.Load<Sprite>(DeckPile.BackResourcePath);
            sr.sprite = back != null ? back : HudFactory.WhiteSprite();
            Vector2 native = sr.sprite.bounds.size;
            go.transform.localScale = new Vector3(CardSize.x / native.x, CardSize.y / native.y, 1f);

            // 카드 안에서 가장 위에 그려지도록 정렬 목록에 넣는다
            CaptureLayerOrders();
            layerRenderers.Add(sr);
            layerRelativeOrders.Add(SortingOrdersPerCard - 1);
            sr.sortingOrder = layerBase * SortingOrdersPerCard + SortingOrdersPerCard - 1;
            return sr;
        }

        /// <summary>카드를 구성하는 모든 렌더러와 원래 그리기 순서를 처음 한 번만 기억해둔다.</summary>
        void CaptureLayerOrders()
        {
            if (layerRenderers != null) return; // 이미 기억해둠
            layerRenderers = new List<Renderer>();
            layerRelativeOrders = new List<int>();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                layerRenderers.Add(r);
                layerRelativeOrders.Add(r.sortingOrder);
            }
        }

        /// <summary>띄어쓰기 기준으로 단어 단위 줄바꿈을 한다. TextMesh는 자동 줄바꿈이 없어서 직접 처리한다.</summary>
        static string WrapText(string s, int maxCharsPerLine)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder();
            int lineLen = 0; // 현재 줄의 글자 수
            foreach (var word in s.Split(' '))
            {
                if (lineLen > 0 && lineLen + 1 + word.Length > maxCharsPerLine)
                {
                    sb.Append('\n'); // 이 단어를 넣으면 넘치므로 줄을 바꾼다
                    lineLen = 0;
                }
                else if (lineLen > 0)
                {
                    sb.Append(' ');  // 같은 줄이면 띄어쓰기로 이어 붙인다
                    lineLen += 1;
                }
                sb.Append(word);
                lineLen += word.Length;
            }
            return sb.ToString();
        }
    }
}
