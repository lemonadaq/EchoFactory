namespace EchoFactory.Core
{
    // First-turn tutorial: a fixed list of hints shown only while turn 1 is being planned and the game is still on.
    public static class TutorialSystem
    {
        public static readonly string[] Steps=
        {
            "1/5 · MAPA: masz jedną działkę z halą produkcyjną. Kolejne działki kupisz tu za kredyty.",
            "2/5 · KONSTRUKCJE: wejdź w halę i sprawdź prasę. Prasa zamienia stal i energię w gotowe produkty oraz złom.",
            "3/5 · RYNEK: produkty nie sprzedają się same. Kupuj brakujące surowce i sprzedawaj gotowe towary.",
            "4/5 · R&D: Hala Badawcza odblokowuje technologie, które zmieniają reguły produkcji.",
            "5/5 · Gotowe? Kliknij ZAKOŃCZ TURĘ. W podsumowaniu zobaczysz, co było wąskim gardłem."
        };

        public static bool IsActive(StrategicState state)
        {
            return state!=null && state.Turn==1 && state.Phase==StrategicPhase.Planning && state.Outcome==GameOutcome.Playing;
        }

        // Returns the hint for a step, or null when the tutorial is not running or the step is past the end.
        public static string GetHint(StrategicState state,int step)
        {
            if(!IsActive(state) || step<0 || step>=Steps.Length)return null;
            return Steps[step];
        }
    }
}
