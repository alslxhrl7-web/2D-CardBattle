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
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
        }

        /// <summary>parent의 자식을 전부 파괴한다. keep(선택)에 대해 true를 돌려주는 자식은 남긴다.</summary>
        public static void DestroyChildren(Transform parent, System.Predicate<Transform> keep = null)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (keep != null && keep(child)) continue;
                DestroySafe(child.gameObject);
            }
        }

        /// <summary>TextMesh에 문구를 넣는다(null이면 조용히 무시 — 인스펙터 연결을 선택 사항으로 두기 위함).</summary>
        public static void SetText(TextMesh target, string text)
        {
            if (target != null) target.text = text;
        }
    }
}
