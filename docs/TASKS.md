# Echo Factory — lista zadań do grywalnej wersji

Jedno zadanie = jedna sesja pracy (człowieka albo asystenta). Bierzemy **pierwsze** zadanie ze statusem *Do zrobienia*, którego zależności są zrobione. Po pracy aktualizujemy status, notatkę i dopisujemy nowe zadania odkryte po drodze.

Statusy: **Do zrobienia** · **W trakcie** · **Zrobione (do testu w Unity)** · **Potwierdzone** (sprawdzone w Unity) · **Zablokowane** (z powodem).
Kto: **Filip** (wymaga Unity / decyzji) · **Asystent** (kod + testy poza Unity).

## Definicja grywalnej wersji (cel)

1. Gra startuje w Unity bez błędów Console; menu → nowa gra / kontynuuj.
2. Pętla tur działa bez martwego punktu: surowce da się pozyskać, produkty sprzedać, a zła strategia może zbankrutować.
3. Gracz widzi w podsumowaniu tury, **co było wąskim gardłem**.
4. Jest cel (np. kontrakty / wynik po N turach) i ekran wygranej oraz przegranej.
5. Zapis i wczytanie gry działają.
6. Hala pozwala stawiać, przesuwać i rozbierać maszyny myszą.
7. Krótki samouczek prowadzi przez pierwszą turę.
8. Build Windows uruchamia się poza edytorem.

## Zasady techniczne

- Reguły gry wyłącznie w `Assets/EchoFactory/Core` (bez `UnityEngine`), każda reguła z testem w `Assets/EchoFactory/Tests`.
- Nowy interfejs wyłącznie w UI Toolkit: układ w `Resources/UI/*.uxml`, wygląd w `Game.uss`, logika w `Runtime/GameUI.cs`. Bez nowego IMGUI.
- Przed commitem: `dotnet run --project Tests/CoreRunner` oraz `dotnet build Tests/UnityCompileCheck` muszą przejść (to samo robi CI „Testy”).
- `UnityCompileCheck` używa bibliotek Unity 2021.3. Nie używać API, którego tam brakuje, i API przestarzałego w Unity 6 (np. `FindObjectOfType`).

## Zadania

| ID | Status | Kto | Zadanie | Warunek ukończenia | Zależy od |
| --- | --- | --- | --- | --- | --- |
| G00 | Zrobione (do testu w Unity) | Asystent | Interfejs strategiczny przeniesiony z IMGUI do UI Toolkit (UXML/USS), widok hali z klikanym stawianiem maszyn, obrazki maszyn w `Art/Machines`. Naprawiono odblokowywanie technologii i niejednoznaczny `Material` w `FactoryGame`. | Ekrany menu, mapa, konstrukcje, hala, R&D, podsumowanie działają w Play. | — |
| U01 | Do zrobienia | Filip | Otworzyć projekt w Unity 6000.3.18f1, wejść w Play, zgłosić błędy Console (issue albo wiadomość). | Lista błędów albo potwierdzenie „brak błędów”. | G00 |
| U02 | Do zrobienia | Filip | Przejść pętlę: nowa gra → mapa → hala → postawienie maszyny → R&D → koniec tury; sprawdzić czytelność w UI Builder (menu **Echo Factory → Edytuj wygląd gry**). | Uwagi do układu i sterowania. | U01 |
| A01 | Zrobione (do testu w Unity) | Asystent | **Pozyskiwanie surowców.** Teraz stal i energia kończą się po kilku turach i gra staje. Posiadane działki wydobywają co turę swój surowiec zależnie od bogactwa (Core + testy), a podsumowanie to pokazuje. | Test: 20 tur startowej strategii bez martwego punktu; liczby w podsumowaniu. Notatka: `ExtractionSystem` — działka daje bogactwo/20 surowca na turę, działka z halą +1 energii z sieci; działka startowa zawsze stalowa. | — |
| A02 | Zrobione (do testu w Unity) | Asystent | **Rynek.** Kupno surowców i sprzedaż gotowych produktów po cenach zależnych od podaży; produkty nie zamieniają się już automatycznie w Credits. Ekran lub panel rynku w UXML. | Testy cen i transakcji; panel działa w UI. Notatka: `MarketSystem` — ceny ±5% za każdy poziom podaży (±10), kupno +10%/sprzedaż −10% od ceny środkowej, podaż wraca o 1 na turę; ekran „RYNEK” (`MarketRow.uxml`). Zmiana: przychód z tury to tylko pasywny (+300 C), więc produkty trzeba sprzedać ręcznie. |  A01 |
| A03 | Zrobione (do testu w Unity) | Asystent | **Raport wąskiego gardła.** `ProductionSystem.Resolve` zapisuje dla każdej maszyny, czy pracowała i czego jej brakło; podsumowanie tury to pokazuje. | Testy powodów (brak stali, energii, technologii); lista w podsumowaniu. Notatka: `MachineReport`/`IdleReason` w `ProductionResult.Reports` i `state.LastMachineReports` (niezapisywane, A04); sekcja „Wąskie gardła” w podsumowaniu. | — |
| A04 | Zrobione (do testu w Unity) | Asystent | **Zapis gry strategicznej.** Zapis po każdej turze, przycisk „KONTYNUUJ” w menu, kopia `.bak`. | Test round-trip w Core; Kontynuuj tylko gdy jest zapis. Notatka: `StrategicSave` (Core, listy zamiast słowników) + `Runtime/StrategicSaveStore` (plik `echo-factory-strategy-v1.json`, odczyt z `.bak` gdy główny uszkodzony); zapis po `EndTurn` i `ContinueToPlanning`. |  — |
| A05 | Zrobione (do testu w Unity) | Asystent | **Cel i koniec gry.** Bankructwo (saldo < 0 przez 2 tury) i cel (np. 50 000 C albo kontrakty) z ekranem wygranej/przegranej. | Testy warunków; ekrany w UXML. Notatka: `GoalSystem` — wygrana przy saldzie ≥ 50 000 C, porażka po 2 turach z rzędu z saldem < 0; `Outcome`/`DebtTurns` w stanie i zapisie; ekran `screen-end`. Kontrakty pominięte (ew. osobne zadanie). | A01, A02 |
| A06 | Zrobione (do testu w Unity) | Asystent | **Edycja hali myszą.** Przeciąganie maszyny (z regułą odstępu) i rozbiórka ze zwrotem 50% ceny. | Testy `FactoryLayoutSystem` (przesunięcie, kolizja, zwrot); obsługa w hali. Notatka: `MoveMachine`/`DemolishMachine` w `FactoryLayoutSystem` (tylko w fazie planowania, ta sama reguła odstępu, zwrot 50% ceny); przyciski PRZESUŃ (klik w nowe miejsce) i ROZBIERZ w panelu wybranej maszyny. | — |
| A07 | Do zrobienia | Asystent | **Hala logistyczna ma funkcję.** Teraz nie przyjmuje maszyn. Np. zwiększa wydobycie/transport między działkami lub odblokowuje sprzedaż hurtową. | Reguła w Core z testem; opis w STRATEGIC_DESIGN. | A01 |
| A08 | Do zrobienia | Asystent | **Samouczek.** Kolejne wskazówki w pierwszej turze (pasek w UXML), możliwe do pominięcia. | Wskazówki prowadzą do końca pierwszej tury. | A03 |
| A09 | Do zrobienia | Asystent | **Balans.** Skrypt w testach symulujący 3 strategie przez 30 tur; żadna nie może być jedyną opłacalną, start nie może bankrutować bez błędów gracza. | Wyniki w VALIDATION; poprawione liczby. | A01, A02, A05 |
| A10 | Do zrobienia | Asystent | **Hala Echo w UI Toolkit.** Przenieść `FactoryGame` (IMGUI) do UXML i połączyć z Halą Produkcyjną zgodnie z STRATEGIC_DESIGN. | Hala Echo działa bez IMGUI; testy Core bez zmian przechodzą. | U01 |
| A11 | Do zrobienia | Asystent | **Menu pauzy i ustawienia.** Esc: wznów / zapisz / menu główne / wyjście; głośność, pełny ekran. | Działa w Play. | A04 |
| U03 | Do zrobienia | Filip | Zbudować prototyp Windows (**Echo Factory → Zbuduj prototyp Windows**) i zagrać 10 tur. | Build startuje; uwagi zapisane. | A04, A05 |
| U04 | Do decyzji | Filip | Czy dodać w CI prawdziwą kompilację Unity (GameCI, wymaga sekretu z licencją Unity)? | Decyzja. | — |

## Jak zgłaszać uwagi

Najprościej: issue na GitHubie z opisem albo zrzutem ekranu (szablon błędu jest w `docs/STATUS.md`). Każda uwaga trafia tutaj jako nowe zadanie.
