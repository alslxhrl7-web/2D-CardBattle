using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// 게임 배경 그림을 깔아준다. Assets/Resources/Backgrounds/BattleBackground.png 를 불러와
    /// 카메라 화면을 꽉 채우도록 늘려서 모든 카드/버튼 뒤에 둔다. (씬에 따로 배치할 필요 없음)
    /// 배경을 바꾸고 싶으면 같은 경로의 그림 파일만 바꾸면 된다.
    /// </summary>
    public static class SceneBackground
    {
        public const string ResourcePath = "Backgrounds/BattleBackground"; // Resources 폴더 기준 경로(확장자 없이)
        const string ObjectName = "GameBackground";                       // 만들어지는 오브젝트 이름
        const int SortingOrder = -300;                                     // 슬롯 표시(-100)보다도 뒤
        static readonly Color Tint = new Color(0.8f, 0.8f, 0.8f, 1f);     // 카드가 잘 보이도록 배경을 살짝 어둡게

        /// <summary>배경이 아직 없으면 만든다. 그림 파일이 없으면 아무 것도 하지 않는다.</summary>
        public static void Ensure(Camera cam)
        {
            if (cam == null || GameObject.Find(ObjectName) != null) return; // 카메라가 없거나 이미 있음

            var sprite = Resources.Load<Sprite>(ResourcePath);
            if (sprite == null) return; // 배경 그림이 없으면 기존 단색 배경 그대로

            var go = new GameObject(ObjectName);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Tint;
            sr.sortingOrder = SortingOrder;

            // 카메라가 보는 영역(세로 = orthographicSize×2, 가로 = 세로×화면비)을 빈틈없이 덮도록 배율 계산
            float viewH = cam.orthographicSize * 2f;
            float viewW = viewH * cam.aspect;
            Vector2 spriteSize = sprite.bounds.size;
            float scale = Mathf.Max(viewW / spriteSize.x, viewH / spriteSize.y);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            go.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 5f); // 카드(z=0)보다 뒤
        }
    }
}
