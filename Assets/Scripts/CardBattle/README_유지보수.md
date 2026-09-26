# CardBattle 스크립트 유지보수 안내

바꾸고 싶은 게 있을 때 어느 파일을 열면 되는지 정리한 문서입니다.

## 처음 한 번 할 일

Unity 상단 메뉴 **CardBattle → 씬 자동 구성**을 누르세요. 코드에는 있지만 씬에 아직 없는 것들을 만들어 연결하고 씬을 저장합니다. 이미 있는 것은 건드리지 않으므로 여러 번 눌러도 안전합니다.

- 필드 슬롯 점검·복구 (FieldSlot 스크립트, 콜라이더, 왼쪽→오른쪽 순서)
- 체력 표시(왼쪽 ±3.7) 2개, 승패 문구(화면 가운데, 평소엔 숨김)
- 조선 기본팩 에셋 `Assets/PackData/JoseonBasicPack.asset`
- "팩 열기" 버튼(오른쪽, 턴 종료 버튼 위)과 팩 결과 패널

위치가 마음에 안 들면 씬에서 오브젝트를 옮기면 됩니다. 코드는 위치에 의존하지 않습니다.

## 무엇을 바꾸려면 어디를 보나

| 바꾸고 싶은 것 | 파일 / 에셋 |
|---|---|
| 체력 30, 군력 최대 10, 손패 최대 10장, 최소 덱 10장, 카드별 최대 매수 | `GameRules.cs` |
| 화면에 나오는 문구 ("군력 1 / 1", "승리!" 등) | `GameTexts.cs` |
| 진영·희귀도·버튼 색 | `GamePalette.cs` |
| 전투 규칙 (누가 누구를 때리는지, Wall/Ranged 효과) | `LaneCombat.cs` |
| 전투에서 효과가 있는 키워드 목록 | `CardKeywords.cs` |
| 상대 AI가 어떤 카드를 어디에 내는지 | `EnemyAI.cs` |
| 카드 한 장의 스탯/설명/그림 | `Assets/CardData/…` 에셋 (코드 수정 불필요) |
| 시작 덱 구성 | `Assets/DeckData/…` 에셋 또는 게임 안 "덱 편집" |
| 카드팩 확률·장수·보장 슬롯 | `Assets/PackData/…` 에셋 (코드 수정 불필요) |
| 카드 모양(초상화 크기, 이름 글자 크기) | `CardView.cs` |

## 자주 하는 작업

**새 카드 추가.** 프로젝트 창 우클릭 → Create → CardBattle → CardData로 만들고 값을 채웁니다. 카드팩에도 넣으려면 팩 에셋을 선택하고 인스펙터 오른쪽 위 ⋮ 메뉴에서 "자동 채우기"를 한 번 더 누릅니다. 덱 빌더 화면에 보이게 하려면 씬의 DeckBuilder 패널에 타일을 추가해야 합니다(현재 12장 고정).

**새 카드팩 (예: 청나라팩).** Create → CardBattle → Card Pack Data로 만들고 `autoFillFaction`을 Qing으로 고른 뒤 "자동 채우기". 버튼까지 만들려면 씬의 PackOpener와 PackOpenButton을 복제하고, 복제한 PackOpener의 `pack` 칸에 새 팩을, 복제한 버튼의 `ui` 칸에 복제한 PackOpener를 넣습니다.

**팩 확률 확인.** 팩 에셋을 선택하고 메뉴 **CardBattle → 카드팩 확률 확인**. 설정 확률과 1000팩 시뮬레이션 결과가 콘솔에 나옵니다. (보장 슬롯 때문에 시뮬레이션 쪽 Elite 이상 비율이 설정보다 높게 나오는 게 정상입니다.)

**새 키워드 효과.** `CardKeywords.cs`에 이름을 추가하고, 효과가 발동하는 곳(대개 `LaneCombat.cs`)에서 `card.HasKeyword(CardKeywords.이름)`으로 확인합니다. 카드 데이터의 `keywordText`가 "이름: 설명" 형식이면 자동으로 인식됩니다.

**새 버튼.** `ClickableButton`을 상속하고 `OnClick()`만 작성합니다. 예시는 `EndTurnButton.cs` (10줄 남짓).

**전투 없이 카드 배치만 테스트.** 씬의 CardManager에서 `Enable Lane Combat` 체크를 끕니다.

## 주의할 점

- 인스펙터에 보이는 필드 이름(`playerHand`, `manaText` 등)을 코드에서 바꾸면 씬 연결이 끊깁니다. 이름을 바꿔야 한다면 `[UnityEngine.Serialization.FormerlySerializedAs("옛이름")]`을 붙이세요.
- MonoBehaviour 클래스 이름과 파일 이름은 같아야 합니다(FieldSlot을 별도 파일로 뺀 이유).
- `Editor` 폴더 안의 스크립트는 에디터 전용이라 게임 빌드에 들어가지 않습니다.

## 파일 구성

- **흐름**: `CardManager.cs` (한 판 진행), `LaneCombat.cs` (전투), `EnemyAI.cs` (상대 판단), `ManaPool.cs` (군력)
- **데이터**: `CardData.cs`, `DeckData.cs`, `CardPackData.cs`, `Faction.cs` (진영/희귀도/편 열거형)
- **보드**: `HandZone.cs`, `FieldZone.cs`, `FieldSlot.cs`, `CardView.cs`, `CardDragHandler.cs`, `CardSlotMover.cs`
- **화면 UI**: `ClickableButton.cs`와 버튼들, `DeckBuilderUI.cs`, `DeckBuilderTile.cs`, `PackOpenerUI.cs`, `CardPackOpener.cs`
- **설정 모음**: `GameRules.cs`, `GameTexts.cs`, `GamePalette.cs`, `CardKeywords.cs`, `UnityUtil.cs`
- **에디터 도구**: `Editor/CardBattleSetupMenu.cs`

## 아직 없는 것

- 보유 카드(컬렉션): 팩에서 나온 카드는 보여주기만 하고 덱 빌더에 반영되지 않습니다.
- 턴 교대: 턴 종료 시 양쪽이 함께 드로우하고, 상대는 바로 카드를 냅니다.
- Wall, Ranged 외 키워드 효과(Charge, Swarm, Rally, Last Stand 등)는 설명만 표시됩니다.
- 드로우 더미가 바닥났을 때의 페널티(탈진).
