# Dziennik agenta

Każdy przebieg agenta (GitHub Actions, `.github/workflows/agent.yml`) dopisuje tu wpis na końcu.

```text
Data / ID zadania:
Zmieniono:
Sprawdzono:
Niesprawdzone (np. wygląd w Unity):
```

## 2026-10-07 — G00 (sesja ręczna)

Zmieniono: interfejs strategiczny w UI Toolkit, poprawki ResearchSystem i FactoryGame, lista zadań, CI „Testy”, workflow agenta.
Sprawdzono: CoreRunner (2139 + 37 asercji), UnityCompileCheck.
Niesprawdzone: uruchomienie w Unity 6000.3.18f1 (U01).

## 2026-10-07 — A01

Zmieniono: nowy `Core/ExtractionSystem.cs` (wydobycie z działek + energia z sieci), wywołanie w `TurnSystem.EndTurn`, `LastExtracted` w stanie, działka startowa = stal, wiersz „Wydobycie” w podsumowaniu (`GameUI.cs`), testy w `StrategyChecks`, docs.
Sprawdzono: CoreRunner (2139 + 82 asercji, w tym 20 tur startu bez martwego punktu), UnityCompileCheck. Brak otwartych issues od uprawnionych autorów (`authorAssociation` niedostępne w `gh issue list`, sprawdzono przez `gh api`).
Niesprawdzone: wygląd podsumowania w Unity; `LastExtracted` nie jest jeszcze serializowany (A04); balans (A09).
