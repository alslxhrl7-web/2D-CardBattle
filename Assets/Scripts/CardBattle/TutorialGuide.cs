namespace CardBattle
{
    /// <summary>
    /// 튜토리얼 진행 순서. 단계마다 안내 문구를 보여주고, 시킨 행동만 할 수 있게 막는다.
    ///   1) 유닛 내기 → 2) 턴 종료 → 3) 장비 놓기 → 4) 턴 종료 → 5) 전술 쓰기 → 6) 자유 플레이
    /// CardManager가 카드를 낼 때 / 턴을 넘길 때 알려주고, 낼 수 있는지 물어본다.
    /// 튜토리얼 덱(Assets/DeckData/Tutorial_*.asset)은 섞지 않으므로, 첫 손패에 의병·편전·봉화가 들어온다.
    /// 단계를 바꾸려면 Step에 이름을 넣고 아래 switch 네 곳에 한 줄씩 추가한다.
    /// </summary>
    public class TutorialGuide
    {
        enum Step { PlaceUnit, EndFirstTurn, Equip, EndSecondTurn, CastSpell, FreePlay }

        Step step = Step.PlaceUnit; // 지금 단계

        /// <summary>튜토리얼이 끝나고 자유롭게 싸우는 중인지.</summary>
        public bool IsFreePlay { get { return step == Step.FreePlay; } }

        /// <summary>지금 단계의 안내 문구.</summary>
        public string Message
        {
            get
            {
                switch (step)
                {
                    case Step.PlaceUnit:     return GameTexts.TutorialPlaceUnit;
                    case Step.EndFirstTurn:  return GameTexts.TutorialEndFirstTurn;
                    case Step.Equip:         return GameTexts.TutorialEquip;
                    case Step.EndSecondTurn: return GameTexts.TutorialEndSecondTurn;
                    case Step.CastSpell:     return GameTexts.TutorialSpell;
                    default:                 return GameTexts.TutorialFreePlay;
                }
            }
        }

        /// <summary>플레이어가 지금 이 카드를 낼 수 있는지. (상대 AI는 묻지 않는다)</summary>
        public bool AllowsCard(CardData card)
        {
            switch (step)
            {
                case Step.PlaceUnit: return card.IsFieldCard;
                case Step.Equip:     return card.IsEquipment;
                case Step.CastSpell: return card.IsSpell;
                case Step.FreePlay:  return true;
                default:             return false; // "턴 종료를 누르세요" 단계에선 카드를 못 낸다
            }
        }

        /// <summary>지금 턴 종료를 눌러도 되는지. (시킨 행동을 하기 전에는 안 됨)</summary>
        public bool AllowsEndTurn
        {
            get { return step == Step.EndFirstTurn || step == Step.EndSecondTurn || step == Step.FreePlay; }
        }

        /// <summary>플레이어가 카드를 냈을 때: 시킨 카드면 다음 단계로.</summary>
        public void OnCardPlayed(CardData card)
        {
            if (step == Step.PlaceUnit && card.IsFieldCard) step = Step.EndFirstTurn;
            else if (step == Step.Equip && card.IsEquipment) step = Step.EndSecondTurn;
            else if (step == Step.CastSpell && card.IsSpell) step = Step.FreePlay;
        }

        /// <summary>플레이어가 턴 종료를 눌렀을 때: 다음 단계로.</summary>
        public void OnTurnEnded()
        {
            if (step == Step.EndFirstTurn) step = Step.Equip;
            else if (step == Step.EndSecondTurn) step = Step.CastSpell;
        }
    }
}
