# CardBattle 스크립트 유지보수 안내

바꾸고 싶은 게 있을 때 어느 파일을 열면 되는지 정리한 문서입니다.

## 게임 시작하는 법

가장 쉬운 방법: Unity 상단 메뉴 **CardBattle → 게임 시작**을 누르세요. 게임 씬(`Assets/Scenes/SampleScene.unity`)을 열고, 씬 구성이 덜 돼 있으면 자동으로 채운 뒤 Play를 눌러줍니다.

직접 하려면:

1. Unity Hub에서 이 프로젝트를 엽니다.
2. 프로젝트 창에서 `Assets/Scenes/SampleScene`을 더블클릭해 엽니다. (다른 씬이 열려 있으면 게임이 안 보입니다)
3. 처음 한 번만 메뉴 **CardBattle → 씬 자동 구성**을 누릅니다.
4. 화면 위쪽 가운데의 ▶(Play) 버튼을 누릅니다. 멈추려면 한 번 더 누릅니다.

Play를 누르면 **처음 화면**이 뜹니다.

- **배틀 시작**: 배틀 화면으로 가서 새 판을 시작합니다(덱 섞기 → 손패 5장 → 상대 첫 수).
- **덱 편집**: 덱 편집 화면으로 갑니다. "저장"을 누르면 처음 화면으로 돌아옵니다.
- **팩 열기**: 조선 기본팩을 열어 결과를 보여줍니다.

배틀 화면에는 카드(손패·필드)와 **턴 종료** 버튼, 군력/덱/체력 표시만 나옵니다. 손패 카드를 필드 빈칸으로 끌어다 놓아 내고(첫 턴에는 1장만), 턴 종료를 누르면 전투 후 다음 턴이 됩니다. 승패가 나면 턴 종료를 한 번 더 눌러 처음 화면으로 돌아갑니다.

화면마다 무엇을 보여줄지는 씬의 `ScreenManager` 오브젝트 인스펙터에서 `Lobby Objects` / `Battle Objects` 목록에 오브젝트를 넣고 빼서 바꿀 수 있습니다. (예: 배틀 중 군력 표시도 숨기고 싶으면 Battle Objects에서 빼고 그 오브젝트를 꺼두면 됨)

## 씬 자동 구성이 하는 일

메뉴 **CardBattle → 씬 자동 구성**은 코드에는 있지만 씬에 아직 없는 것들을 만들어 연결하고 씬을 저장합니다. 이미 있는 것은 건드리지 않으므로 여러 번 눌러도 안전합니다.

- 필드 슬롯 점검·복구 (FieldSlot 스크립트, 콜라이더, 왼쪽→오른쪽 순서)
- 체력 표시(왼쪽 ±3.7) 2개, 승패 문구(화면 가운데, 평소엔 숨김)
- 조선 기본팩 에셋 `Assets/PackData/JoseonBasicPack.asset`
- "팩 열기" 버튼과 팩 결과 패널
- 화면 나누기: `ScreenManager`, 처음 화면(`Lobby`: 제목·배틀 시작·덱 편집·팩 열기), 배틀 화면 오브젝트 목록

위치가 마음에 안 들면 씬에서 오브젝트를 옮기면 됩니다. 코드는 위치에 의존하지 않습니다.

## 무엇을 바꾸려면 어디를 보나

| 바꾸고 싶은 것 | 파일 / 에셋 |
|---|---|
| 체력 30, 군력 최대 10, 손패 최대 10장, 최소 덱 10장, 카드별 최대 매수, 첫 턴에 낼 수 있는 장수 | `GameRules.cs` |
| 화면마다 보이는 오브젝트 | 씬의 `ScreenManager` 인스펙터 (`ScreenManager.cs`) |
| 화면에 나오는 문구 ("군력 1 / 1", "승리!" 등) | `GameTexts.cs` |
| 진영·희귀도·버튼 색 | `GamePalette.cs` |
| 전투 규칙 (누가 누구를 때리는지, Wall/Ranged 효과) | `LaneCombat.cs` |
| 전투에서 효과가 있는 키워드 목록 | `CardKeywords.cs` |
| 상대 AI가 어떤 카드를 어디에 내는지 | `EnemyAI.cs` |
| 카드 한 장의 스탯/설명/그림 | `Assets/CardData/…` 에셋 (코드 수정 불필요) |
| 시작 덱 구성 | `Assets/DeckData/…` 에셋 또는 게임 안 "덱 편집" |
| 카드팩 확률·장수·보장 슬롯 | `Assets/PackData/…` 에셋 (코드 수정 불필요) |
| 카드 글자 크기(코스트, 이름, 설명 등) | `CardView.cs` 위쪽 "글자 크기" 숫자들 |
| 카드 모양(초상화 크기 등) | `CardView.cs` |
| 전투 연출 속도·거리(돌진, 피해 숫자, 사라짐) | `CombatAnimation.cs` 위쪽 숫자들 |
| 카드에 마우스를 올렸을 때 크게 보이는 확대 카드의 위치·배율 | `CardHoverPreview.cs` 위쪽 숫자들 |
| 체력 표시 위치, 승리/패배 배너 크기·위치 | `CardManager.cs` 위쪽 "실행 중 자동 생성 표시" 숫자들 |
| 승리/패배/무승부 글자 색, 배너 배경색 | `GamePalette.cs` (`ResultVictory` 등) |
| 배경 그림 | `Assets/Resources/Backgrounds/BattleBackground.png` 파일을 같은 이름으로 교체 (밝기는 `SceneBackground.cs`의 `Tint`) |

## 자주 하는 작업

**새 카드 추가.** 프로젝트 창 우클릭 → Create → CardBattle → CardData로 만들고 값을 채웁니다. 카드팩에도 넣으려면 팩 에셋을 선택하고 인스펙터 오른쪽 위 ⋮ 메뉴에서 "자동 채우기"를 한 번 더 누릅니다. 덱 빌더 화면에 보이게 하려면 씬의 DeckBuilder 패널에 타일을 추가해야 합니다(현재 12장 고정).

**새 카드팩 (예: 청나라팩).** Create → CardBattle → Card Pack Data로 만들고 `autoFillFaction`을 Qing으로 고른 뒤 "자동 채우기". 버튼까지 만들려면 씬의 PackOpener와 PackOpenButton을 복제하고, 복제한 PackOpener의 `pack` 칸에 새 팩을, 복제한 버튼의 `ui` 칸에 복제한 PackOpener를 넣습니다.

**팩 확률 확인.** 팩 에셋을 선택하고 메뉴 **CardBattle → 카드팩 확률 확인**. 설정 확률과 1000팩 시뮬레이션 결과가 콘솔에 나옵니다. (보장 슬롯 때문에 시뮬레이션 쪽 Elite 이상 비율이 설정보다 높게 나오는 게 정상입니다.)

**새 키워드 효과.** `CardKeywords.cs`에 이름을 추가하고, 효과가 발동하는 곳(대개 `LaneCombat.cs`)에서 `card.HasKeyword(CardKeywords.이름)`으로 확인합니다. 카드 데이터의 `keywordText`가 "이름: 설명" 형식이면 자동으로 인식됩니다.

**새 버튼.** `ClickableButton`을 상속하고 `OnClick()`만 작성합니다. 예시는 `EndTurnButton.cs` (10줄 남짓).

**전투 없이 카드 배치만 테스트.** 씬의 CardManager에서 `Enable Lane Combat` 체크를 끕니다. 전투 연출만 끄려면 `Play Combat Animation` 체크를 끕니다(결과는 바로 반영).

**카드 설명 읽기.** 카드(손패·필드·덱 편집 타일)에 마우스를 올리면 화면 왼쪽에 3배 크기로 확대돼 설명이 보입니다. 마우스를 떼면 사라집니다.

**체력·승패 표시.** 씬에 체력 글자나 승패 글자가 없어도 게임 시작 시 자동으로 만들어집니다(내 체력 왼쪽 아래, 적 체력 왼쪽 위). 한쪽 체력이 0이 되면 화면 가운데에 "승리!" 또는 "패배..." 배너가 뜨고, 턴 종료 버튼을 누르면 처음 화면(없으면 새 판)으로 갑니다.

## 주의할 점

- 인스펙터에 보이는 필드 이름(`playerHand`, `manaText` 등)을 코드에서 바꾸면 씬 연결이 끊깁니다. 이름을 바꿔야 한다면 `[UnityEngine.Serialization.FormerlySerializedAs("옛이름")]`을 붙이세요.
- MonoBehaviour 클래스 이름과 파일 이름은 같아야 합니다(FieldSlot을 별도 파일로 뺀 이유).
- `Editor` 폴더 안의 스크립트는 에디터 전용이라 게임 빌드에 들어가지 않습니다.
- 배경 그림은 `Resources` 폴더 안에 있어야 코드가 불러올 수 있습니다. 파일 이름·위치를 바꾸면 `SceneBackground.cs`의 `ResourcePath`도 같이 바꾸세요.

## 파일 구성

- **흐름**: `CardManager.cs` (한 판 진행), `LaneCombat.cs` (전투 규칙), `CombatAnimation.cs` (전투 연출), `EnemyAI.cs` (상대 판단), `ManaPool.cs` (군력)
- **데이터**: `CardData.cs`, `DeckData.cs`, `CardPackData.cs`, `Faction.cs` (진영/희귀도/편 열거형)
- **보드**: `HandZone.cs`, `FieldZone.cs`, `FieldSlot.cs`, `CardView.cs`, `CardDragHandler.cs`, `CardSlotMover.cs`
- **화면 UI**: `ScreenManager.cs`(화면 전환), `ScreenButton.cs`, `ClickableButton.cs`와 버튼들, `DeckBuilderUI.cs`, `DeckBuilderTile.cs`, `PackOpenerUI.cs`, `CardPackOpener.cs`
- **화면 표시 도우미**: `CardHoverPreview.cs` (마우스 올리면 카드 확대), `HudFactory.cs` (실행 중 글자/배경판 생성), `SceneBackground.cs` (배경 그림 깔기)
- **설정 모음**: `GameRules.cs`, `GameTexts.cs`, `GamePalette.cs`, `CardKeywords.cs`, `UnityUtil.cs`
- **에디터 도구**: `Editor/CardBattleSetupMenu.cs` (게임 시작 / 씬 자동 구성 / 카드팩 확률 확인 메뉴), `Editor/CardBattleAutoApply.cs` (스크립트가 바뀌어 컴파일되면 씬 자동 구성을 한 번 알아서 실행하고 결과를 `Logs/CardBattleAutoApply.log`에 기록. 다시 돌리려면 메뉴 CardBattle → 자동 적용 다시 실행)

## 아직 없는 것

- 보유 카드(컬렉션): 팩에서 나온 카드는 보여주기만 하고 덱 빌더에 반영되지 않습니다.
- 턴 교대: 턴 종료 시 양쪽이 함께 드로우하고, 상대는 바로 카드를 냅니다.
- Wall, Ranged 외 키워드 효과(Charge, Swarm, Rally, Last Stand 등)는 설명만 표시됩니다.
- 드로우 더미가 바닥났을 때의 페널티(탈진).
