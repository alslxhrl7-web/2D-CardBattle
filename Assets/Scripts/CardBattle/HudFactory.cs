using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 게임 실행 중에 화면 글자/배경판을 만들어주는 도우미.
    /// 메뉴 "씬 자동 구성"을 누르지 않았더라도 체력 표시·승패 배너가 빠지지 않도록 CardManager가 사용한다.
    /// </summary>
    public static class HudFactory
    {
        static Sprite whiteSprite; // 배경판용 흰색 1×1 스프라이트 (처음 한 번 만들어 재사용)

        /// <summary>
        /// 글자 오브젝트를 만든다. style이 있으면 그 글꼴/재질/크기를 그대로 복제하고(가장 확실하게 글자가 보임),
        /// 없으면 빈 TextMesh를 새로 만든다.
        /// </summary>
        public static TextMesh CreateLabel(TextMesh style, string name, Vector3 position)
        {
            GameObject go;
            if (style != null)
            {
                go = Object.Instantiate(style.gameObject);
                go.transform.SetParent(null, false);
                go.SetActive(true);
            }
            else
            {
                go = new GameObject(name, typeof(MeshRenderer), typeof(TextMesh));
            }
            go.name = name;
            go.transform.position = position;
            go.transform.rotation = Quaternion.identity;
            var text = go.GetComponent<TextMesh>();
            text.text = "";
            return text;
        }

        /// <summary>글자 높이(월드 유닛)를 정한다. 글꼴 해상도를 크게 두고 characterSize로 줄여서 선명하게.</summary>
        public static void SetHeight(TextMesh text, float height, bool bold)
        {
            if (text == null) return;
            text.fontSize = 64;
            text.characterSize = height * 10f / 64f; // TextMesh 글자 높이 ≈ fontSize × characterSize ÷ 10
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        }

        /// <summary>
        /// parent 아래에 반투명 사각 배경판을 만든다(이미 같은 이름이 있으면 그걸 돌려줌).
        /// size는 월드 유닛 가로×세로, sortingOrder는 그 위의 글자보다 작게 줄 것.
        /// </summary>
        public static SpriteRenderer EnsureBackdrop(Transform parent, string name, Vector2 size, Color color, int sortingOrder)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.GetComponent<SpriteRenderer>();

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0.1f); // 글자 살짝 뒤
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        /// <summary>흰색 1×1(월드 유닛) 스프라이트. 색을 입혀 배경판으로 쓴다.</summary>
        public static Sprite WhiteSprite()
        {
            if (whiteSprite == null)
            {
                var tex = Texture2D.whiteTexture; // Unity 기본 흰 텍스처(4×4)
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }
            return whiteSprite;
        }
    }
}
