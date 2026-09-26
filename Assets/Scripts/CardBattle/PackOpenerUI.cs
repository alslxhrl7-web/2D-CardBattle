using UnityEngine;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 카드팩 개봉 결과 패널. OpenPack()이 CardPackOpener로 카드를 뽑아 한 줄로 펼쳐 보여주고,
    /// ClosePanel()이 패널을 닫으며 진열한 카드를 정리한다.
    /// 다른 팩용 버튼을 만들고 싶으면 이 오브젝트를 복제하고 pack 필드만 바꾸면 된다.
    ///
    /// 참고: 아직 "보유 카드(컬렉션)" 개념이 없어서, 뽑힌 카드는 보여주기만 하고 덱 빌더에는 반영되지 않는다.
    /// </summary>
    public class PackOpenerUI : MonoBehaviour
    {
        [Header("연결")]
        public GameObject panelRoot;      // 결과 패널(평소 비활성)
        public CardManager manager;       // 카드 프리팹을 만들 때 사용
        public Transform revealParent;    // 뽑힌 카드들이 놓일 위치(패널 안)
        public TextMesh titleText;        // "조선 기본팩 개봉! (5장)"

        [Header("팩")]
        public CardPackData pack;

        [Header("배치")]
        public float cardSpacing = 2.2f;  // 뽑힌 카드 사이 가로 간격(월드 유닛)

        // 패널 배경(sortingOrder 30000대)보다 위에 그려지도록 하는 카드 정렬 버킷 시작값
        // (CardView.SetLayerBase: 버킷 × 20 → 1600 × 20 = 32000)
        public const int RevealLayerBase = 1600;

        readonly List<GameObject> spawned = new List<GameObject>();

        void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void OpenPack()
        {
            if (pack == null || manager == null || revealParent == null) return;

            ClearReveal();
            if (panelRoot != null) panelRoot.SetActive(true); // 먼저 켜야 카드 이동 코루틴 등이 정상 동작

            var drawn = CardPackOpener.Open(pack);
            float mid = (drawn.Count - 1) / 2f;
            for (int i = 0; i < drawn.Count; i++)
            {
                var view = manager.SpawnCardView(drawn[i], revealParent.position, revealParent);
                if (view == null) continue;
                view.gameObject.name = "PackResult_" + drawn[i].cardNameEn;
                view.transform.localPosition = new Vector3((i - mid) * cardSpacing, 0f, -i * 0.01f);
                view.transform.localRotation = Quaternion.identity;
                view.SetLayerBase(RevealLayerBase + i);
                spawned.Add(view.gameObject);
            }

            UnityUtil.SetText(titleText, string.Format(GameTexts.PackOpened, pack.packName, drawn.Count));
        }

        public void ClosePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            ClearReveal();
        }

        void ClearReveal()
        {
            foreach (var go in spawned) UnityUtil.DestroySafe(go);
            spawned.Clear();
        }
    }
}
