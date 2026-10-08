namespace EchoFactory.Core
{
    // Win/lose rules: reach WinCredits to win; Credits below zero for BankruptcyTurns turns in a row means bankruptcy.
    public static class GoalSystem
    {
        public const int WinCredits=50000;
        public const int BankruptcyTurns=2;

        public static void Evaluate(StrategicState state)
        {
            if(state.Outcome!=GameOutcome.Playing)return;
            state.DebtTurns=state.Credits<0?state.DebtTurns+1:0;
            if(state.DebtTurns>=BankruptcyTurns)state.Outcome=GameOutcome.Lost;
            else if(state.Credits>=WinCredits)state.Outcome=GameOutcome.Won;
        }
    }
}
