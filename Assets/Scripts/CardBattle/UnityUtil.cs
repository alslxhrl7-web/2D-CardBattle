using UnityEngine;

namespace CardBattle
{
    /// <summary>여러 스크립트에서 같이 쓰는 작은 Unity 도우미 함수 모음.</summary>
    public static class UnityUtil
    {
        /// <summary>
        /// 플레이 모드/에디트 모드 어디서 호출해도 안전하게 오브젝트를 파괴한다.
        /// (Object.Destroy는 에디트 모드에서 쓰면 "Destroy may not be called from edit mode!" 오류가 난다)
        /// </summary>
        public static void DestroySafe(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Object.Destroy(go);  // 플레이 중: 프레임 끝에 파괴
            else Object.DestroyImmediate(go);               // 에디터: 즉시 파괴
        }

        /// <summary>parent의 자식을 전부 파괴한다. keep(선택)이 true를 돌려주는 자식은 남긴다.</summary>
        public static void DestroyChildren(Transform parent, System.Predicate<Transform> keep = null)
        {
            if (parent == null) return;
            // 뒤에서부터 지워야 인덱스가 밀리지 않는다
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (keep != null && keep(child)) continue; // 남겨야 하는 자식은 건너뜀
                DestroySafe(child.gameObject);
            }
        }

        /// <summary>TextMesh에 문구를 넣는다. target이 비어있으면(인스펙터에 연결 안 됨) 조용히 무시한다.</summary>
        public static void SetText(TextMesh target, string text)
        {
            if (target != null) target.text = text;
        }

        const int TextFontSize = 64; // 글꼴 해상도. 크게 두고 characterSize로 줄여야 글자가 선명하다

        /// <summary>
        /// 글자 높이(월드 유닛)와 굵기를 정한다. TextMesh 글자 높이 ≈ fontSize × characterSize ÷ 10.
        /// 카드 글자·체력 표시·승패 배너·덱 더미 글자가 모두 이 함수를 쓴다.
        /// </summary>
        public static void SetTextHeight(TextMesh text, float height, bool bold)
        {
            if (text == null) return;
            text.fontSize = TextFontSize;
            text.characterSize = height * 10f / TextFontSize;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        }
    }
}
