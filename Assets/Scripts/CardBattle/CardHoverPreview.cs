using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 카드에 마우스를 올리면 화면 왼쪽에 그 카드를 크게 보여주는 미리보기.
    /// 카드가 작아서 설명 글자가 안 읽힐 때, 올려두기만 하면 큰 카드로 설명을 읽을 수 있다.
    /// 손패·필드·팩 결과·덱 편집 타일 카드에 자동으로 붙는다(상대 손패 카드는 보여주지 않음).
    /// 미리보기 위치/크기는 아래 상수만 바꾸면 된다.
    /// </summary>
    public class CardHoverPreview : MonoBehaviour
    {
        // ---- 미리보기 위치/크기 ----
        static readonly Vector3 PreviewPosition = new Vector3(-9.2f, 0.5f, -1f); // 화면 왼쪽 가운데
        const float PreviewScale = 3f;       // 원래 카드의 몇 배로 크게 보여줄지
        const int PreviewLayerBase = 1610;   // 다른 카드·패널보다 위에 그리기 위한 정렬 구간 (×20 = 32200)

        /// <summary>카드 프리팹과 상대 손패를 알기 위한 매니저 (CardManager가 시작할 때 넣어준다).</summary>
        public static CardManager manager;

        static GameObject previewObject;        // 지금 떠 있는 미리보기 카드 (없으면 null)
        static CardHoverPreview previewOwner;   // 어느 카드의 미리보기인지

        CardView view; // 이 카드의 표시 컴포넌트

        /// <summary>같은 오브젝트의 CardView를 찾아둔다.</summary>
        void Awake()
        {
            view = GetComponent<CardView>();
        }

        /// <summary>마우스가 카드 위에 올라왔을 때: 크게 보여준다.</summary>
        void OnMouseEnter()
        {
            if (view == null || view.data == null) return;
            if (IsEnemyHandCard()) return; // 상대 손패는 비밀
            Show();
        }

        /// <summary>마우스가 카드에서 벗어났을 때: 미리보기를 지운다.</summary>
        void OnMouseExit()
        {
            if (previewOwner == this) Hide();
        }

        /// <summary>카드가 꺼지거나 파괴될 때(화면 전환, 전투에서 죽음 등): 미리보기도 지운다.</summary>
        void OnDisable()
        {
            if (previewOwner == this) Hide();
        }

        /// <summary>이 카드가 상대 손패에 들어있는지.</summary>
        bool IsEnemyHandCard()
        {
            return manager != null && manager.enemyHand != null && transform.parent == manager.enemyHand.transform;
        }

        /// <summary>이 카드의 큰 복사본을 만들어 왼쪽에 띄운다.</summary>
        void Show()
        {
            Hide();
            if (manager == null || manager.cardViewPrefab == null) return;

            previewObject = Object.Instantiate(manager.cardViewPrefab, PreviewPosition, Quaternion.identity);
            previewObject.name = "CardPreview";
            previewObject.transform.localScale = Vector3.one * PreviewScale;

            var pv = previewObject.GetComponent<CardView>();
            if (pv != null)
            {
                pv.Setup(view.data);
                // 필드에서 체력이 깎였거나 장비로 스탯이 올랐으면 지금 값을 보여준다 (장비·전술 카드는 적힌 값 그대로)
                bool changed = view.currentHealth != view.data.health || view.currentAttack != view.data.attack;
                if (view.data.IsFieldCard && view.currentHealth > 0 && changed)
                {
                    pv.currentAttack = view.currentAttack;
                    pv.currentHealth = view.currentHealth;
                    pv.RefreshStatTexts();
                }
                pv.SetLayerBase(PreviewLayerBase);
            }

            // 미리보기가 마우스 클릭을 가로채지 않도록 콜라이더를 끈다
            foreach (var col in previewObject.GetComponentsInChildren<Collider2D>(true)) col.enabled = false;

            previewOwner = this;
        }

        /// <summary>떠 있는 미리보기를 지운다.</summary>
        public static void Hide()
        {
            if (previewObject != null) UnityUtil.DestroySafe(previewObject);
            previewObject = null;
            previewOwner = null;
        }

        /// <summary>카드에 미리보기 기능이 없으면 붙여준다.</summary>
        public static void AttachTo(GameObject card)
        {
            if (card != null && card.GetComponent<CardHoverPreview>() == null) card.AddComponent<CardHoverPreview>();
        }
    }
}
