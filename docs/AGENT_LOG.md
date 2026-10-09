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

## 2026-10-08 — A02

Zmieniono: nowy `Core/MarketSystem.cs` (ceny z podaży, kupno/sprzedaż, regeneracja co turę), `MarketSupply` w stanie, usunięta automatyczna sprzedaż produktów w `TurnSystem` (przychód = tylko pasywny), ekran „RYNEK” (`Game.uxml`, `MarketRow.uxml`, `GameUI.cs`), testy w `StrategyChecks` (poprzednie oczekiwane wartości przychodu 420 → 300 i 10220 → 10100 zmienione celowo wraz z regułą), docs.
Sprawdzono: CoreRunner (2139 + 116 asercji, w tym 20 tur z ręczną sprzedażą), UnityCompileCheck. Brak otwartych issues z etykietą `agent`.
Niesprawdzone: wygląd i działanie ekranu rynku w Unity; `MarketSupply` nie jest serializowany (A04); balans cen (A09); `LastTurnProduction` w podsumowaniu nie pokazuje już przychodu z produktów — wąskie gardło to A03.

## 2026-10-08 — A03

Zmieniono: `MachineReport`/`IdleReason` w `ProductionSystem` (powód bezczynności każdej maszyny), `LastMachineReports` w stanie i `TurnSystem`, sekcja „Wąskie gardła” w podsumowaniu (`GameUI.cs`), testy w `StrategyChecks`, docs.
Sprawdzono: CoreRunner, UnityCompileCheck (wyniki poniżej w commicie). Brak otwartych issues z etykietą `agent`.
Niesprawdzone: wygląd podsumowania w Unity; `LastMachineReports` nie jest serializowany (A04); werdykt „SYSTEM DZIAŁA” nadal opiera się na produkcji > 0.

## 2026-10-08 — A04

Zmieniono: `Core/StrategicSave.cs` (migawka stanu z walidacją, w tym raporty wąskich gardeł, podaż rynku, ostatnie wydobycie), `Runtime/StrategicSaveStore.cs` (zapis atomowy + `.bak`), przycisk „KONTYNUUJ” (`Game.uxml`, `GameUI.cs`), zapis po każdej turze, testy round-trip w `StrategyChecks`, docs.
Sprawdzono: CoreRunner (2139 + 130 asercji), UnityCompileCheck. Brak otwartych issues z etykietą `agent`.
Niesprawdzone: faktyczny zapis/odczyt JsonUtility w Unity (test Core nie używa JsonUtility); wygląd przycisku w menu; komunikat o błędzie wczytania na ekranie menu (etykieta `lbl-message` może być ukryta w menu); zapis nie obejmuje Hali Echo.

## 2026-10-08 — A05

Zmieniono: `Core/GoalSystem.cs` (cel 50 000 C, bankructwo po 2 turach ujemnego salda), `Outcome`/`DebtTurns` w `StrategicState` i `StrategicSave` (bez zmiany wersji — stare zapisy czytają się jako „gra trwa”), blokada `EndTurn`/`ContinueToPlanning` po końcu gry, ekran końca gry (`Game.uxml`, `GameUI.cs`), cel i ostrzeżenie o długu w podsumowaniu, testy w `StrategyChecks`.
Sprawdzono: CoreRunner (2139 + 140 asercji), UnityCompileCheck. Brak otwartych issues z etykietą `agent`.
Niesprawdzone: wygląd i przepływ ekranu końca gry w Unity; zapis końcowego stanu pozwala „Kontynuuj” trafić prosto na ekran końca; osiągalność celu 50 000 C i balans (A09); kontrakty nie zostały zrobione.

## 2026-10-09 — A06

Zmieniono: `FactoryLayoutSystem` (`CanMoveMachine`/`MoveMachine`/`DemolishMachine`/`GetRefund`, wspólna stała odstępu), przyciski PRZESUŃ i ROZBIERZ (`Game.uxml`, `GameUI.cs`), testy w `StrategyChecks`.
Sprawdzono: CoreRunner (2139 + 150 asercji), UnityCompileCheck. Brak otwartych issues z etykietą `agent`.
Niesprawdzone: działanie i wygląd w Unity; przesuwanie jest „kliknij maszynę → PRZESUŃ → kliknij miejsce” (nie drag & drop); rozbiórka nie ma potwierdzenia.
