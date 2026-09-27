using UnityEngine;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// 효과음·배경음 재생 담당. 어디서든 GameAudio.Play(GameAudio.CardPlace) 처럼 한 줄로 부르면 된다.
    /// 씬에 따로 놓을 필요 없이, 처음 소리를 낼 때 "GameAudio" 오브젝트를 스스로 만든다.
    ///
    /// 소리 파일 위치 (파일 이름 = 아래 상수 이름, 확장자 .wav):
    ///   Assets/Resources/Sounds/            공통 효과음 (카드 소환, 드로우, 승리 …), 타격음 Hit_1·Hit_2, 배경음 BattleAmbience
    ///   Assets/Resources/Sounds/Attacks/    유닛별 공격음. 파일 이름 = 카드 에셋 이름 (예: Im_Gyeongeop.wav)
    ///                                       파일이 없는 카드는 Default.wav를 쓴다
    /// 소리를 바꾸고 싶으면 같은 이름의 파일로 덮어쓰기만 하면 된다(코드 수정 불필요).
    /// 파일이 없으면 그 소리만 조용히 건너뛴다(오류 없음).
    /// </summary>
    public static class GameAudio
    {
        // ---- 파일 위치 ----
        public const string Folder = "Sounds/";                // Resources 안의 공통 효과음 폴더
        public const string AttackFolder = "Sounds/Attacks/";  // Resources 안의 유닛별 공격음 폴더
        public const string DefaultAttack = "Default";         // 공격음 파일이 없는 카드가 쓰는 소리

        // ---- 공통 효과음 이름 (= 파일 이름) ----
        public const string CardPlace = "CardPlace";  // 유닛·진 카드를 필드에 냄
        public const string CardDraw = "CardDraw";    // 카드 드로우
        public const string Shuffle = "Shuffle";      // 새 판 시작 (덱 섞기)
        public const string Equip = "Equip";          // 장비 장착
        public const string Spell = "Spell";          // 전술 사용
        public const string UnitDeath = "UnitDeath";  // 유닛이 죽어 사라짐
        public const string WallBlock = "WallBlock";  // 방어로 피해가 0이 됨
        public const string Victory = "Victory";      // 승리
        public const string Defeat = "Defeat";        // 패배
        public const string Draw = "Draw";            // 무승부
        public const string Click = "Click";          // 버튼 클릭
        public const string PackOpen = "PackOpen";    // 카드팩 개봉
        public const string Ambience = "BattleAmbience"; // 배틀 배경음 (반복 재생)

        /// <summary>타격음. 맞을 때마다 순서대로 번갈아 쓴다.</summary>
        public static readonly string[] HitSounds = { "Hit_1", "Hit_2" };

        // ---- 음량 (0~1) ----
        public static float effectVolume = 1f;    // 효과음 크기
        public static float ambienceVolume = 0.6f; // 배경음 크기

        const float SameSoundGap = 0.05f; // 같은 소리가 이 시간(초) 안에 또 불리면 한 번만 낸다 (손패 여러 장 동시 드로우 등)

        static AudioSource effectSource;   // 효과음용 (여러 소리를 겹쳐 낼 수 있음)
        static AudioSource ambienceSource; // 배경음용 (반복 재생)
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();  // 한 번 읽은 소리 보관
        static readonly Dictionary<string, float> lastPlayTime = new Dictionary<string, float>();   // 소리별 마지막 재생 시각
        static int nextHit; // 다음에 쓸 타격음 번호

        // ================= 재생 =================

        /// <summary>공통 효과음 하나를 낸다. name은 위 상수 중 하나.</summary>
        public static void Play(string name)
        {
            PlayClip(Load(Folder + name), name);
        }

        /// <summary>이 카드의 공격음을 낸다. 카드 전용 파일이 없으면 Default.wav.</summary>
        public static void PlayAttack(CardData card)
        {
            if (!Application.isPlaying) return;
            string path = AttackPath(card);
            var clip = Load(path);
            if (clip == null) clip = Load(AttackFolder + DefaultAttack); // 전용 공격음이 없는 카드
            PlayClip(clip, path);
        }

        /// <summary>타격음을 낸다 (Hit_1 → Hit_2 → Hit_1 … 번갈아).</summary>
        public static void PlayHit()
        {
            Play(NextHitSound());
        }

        /// <summary>배틀 배경음을 처음부터 반복 재생한다.</summary>
        public static void StartAmbience()
        {
            if (!Application.isPlaying) return;
            var clip = Load(Folder + Ambience);
            if (clip == null) return;
            EnsureSources();
            ambienceSource.clip = clip;
            ambienceSource.volume = ambienceVolume;
            ambienceSource.Play();
        }

        /// <summary>배경음을 멈춘다.</summary>
        public static void StopAmbience()
        {
            if (ambienceSource != null) ambienceSource.Stop();
        }

        // ================= 이름 계산 (테스트에서도 사용) =================

        /// <summary>카드의 공격음 파일 경로 (Resources 기준, 확장자 없음). 예: "Sounds/Attacks/Dodo"</summary>
        public static string AttackPath(CardData card)
        {
            return AttackFolder + (card != null ? card.name : DefaultAttack);
        }

        /// <summary>이번에 쓸 타격음 이름을 돌려주고 다음 순서로 넘긴다.</summary>
        public static string NextHitSound()
        {
            string name = HitSounds[nextHit % HitSounds.Length];
            nextHit = (nextHit + 1) % HitSounds.Length;
            return name;
        }

        // ================= 내부 동작 =================

        /// <summary>소리 하나를 효과음 채널로 낸다. 같은 소리를 너무 짧은 간격으로 부르면 무시한다.</summary>
        static void PlayClip(AudioClip clip, string key)
        {
            if (!Application.isPlaying || clip == null) return;

            float now = Time.unscaledTime;
            float last;
            if (lastPlayTime.TryGetValue(key, out last) && now - last < SameSoundGap) return; // 방금 낸 소리
            lastPlayTime[key] = now;

            EnsureSources();
            effectSource.PlayOneShot(clip, effectVolume);
        }

        /// <summary>Resources에서 소리를 읽는다(한 번 읽은 것은 보관해 두고 다시 쓴다). 없으면 null.</summary>
        static AudioClip Load(string path)
        {
            AudioClip clip;
            if (!clips.TryGetValue(path, out clip))
            {
                clip = Resources.Load<AudioClip>(path);
                clips[path] = clip; // 없는 파일도 기록해서 매번 찾지 않게
            }
            return clip;
        }

        /// <summary>소리를 낼 오브젝트가 없으면 만든다. 씬이 바뀌어도 남아 있다.</summary>
        static void EnsureSources()
        {
            if (effectSource != null) return;
            var go = new GameObject("GameAudio");
            Object.DontDestroyOnLoad(go);
            effectSource = go.AddComponent<AudioSource>();
            effectSource.playOnAwake = false;
            ambienceSource = go.AddComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.loop = true;
        }
    }
}
