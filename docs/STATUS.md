# Echo Factory — stan projektu i wspólna lista prac

Ostatnia aktualizacja: **9 września 2026**. Baza kodu opisana poniżej: commit [`90199a5`](https://github.com/lemonadaq/EchoFactory/commit/90199a529689ae2cd4b8a3fb1b51487f82a0ce2f), gałąź `main`.

**Jesteśmy na etapie pierwszego prototypu Unity. Rdzeń C# przeszedł testy; projekt nie został jeszcze uruchomiony w edytorze Unity.** Najbliższy cel to otworzyć halę, sprawdzić pełną pętlę i naprawić problemy z pierwszej sesji. Nie traktujemy istniejącego kodu interfejsu i zapisu jako potwierdzonego działania w Unity.

Ten plik jest punktem startowym dla Filipa i kolejnych sesji pracy z asystentem. Aktualizujemy go po zmianach i testach, żeby nie odtwarzać stanu projektu z rozmów.

## Szybki powrót do pracy

- Repozytorium: `lemonadaq/EchoFactory`; zapis przez integrację GitHub działa.
- Przypięty edytor: **Unity 6000.3.18f1, rodzina 6.3 LTS**.
- Główna scena: `Assets/EchoFactory/Scenes/Factory.unity`.
- Otwarcie sceny: **Echo Factory → Otwórz halę**, następnie **Play**.
- Testy logiki w edytorze: **Echo Factory → Uruchom testy logiki**; wynik w Console.
- Wynik już wykonanych testów poza Unity: **2079 asercji, 1000 deterministycznych powtórek**.
- Najbliższe zadanie: **U01 — instalacja i pierwszy import w Unity**.
- Przygotowany wcześniej prototyp przeglądarkowy jest materiałem porównawczym. Dalszy rozwój prowadzimy w tym projekcie C#.

Dokumenty pomocnicze: [uruchomienie i sterowanie](../README.md), [reguły projektu](DESIGN.md), [scenariusz pierwszej sesji](PLAYTEST.md), [dowody i granice testów](VALIDATION.md).

## Jak czytać status

| Status | Znaczenie |
| --- | --- |
| Sprawdzone w Core | Kod C# istnieje i objęte nim scenariusze przeszły testy poza Unity. Nie potwierdza to działania UI. |
| Zaimplementowane, do testu | Kod lub pliki są przygotowane; działanie w środowisku docelowym nie zostało potwierdzone. |
| Do zrobienia | Zadanie nie zostało wykonane. |
| Do decyzji | Pomysł lub szczegół wymagający wyboru; nie stanowi zatwierdzonego zakresu. |
| Odłożone | Świadomie poza bieżącą halą; wracamy do niego po podstawowej pętli. |

## Co już jest zrobione

### Rdzeń i rozgrywka

| Obszar | Zrobione | Weryfikacja / ograniczenie |
| --- | --- | --- |
| Architektura | Osobne assembly Core, Runtime, Editor i testy. Core nie zależy od UnityEngine. | Core skompilowany; Runtime i Editor do kompilacji w Unity. |
| Symulacja | Stałe 20 ticków/s; zmiana 60 s; jawna kolejność zdarzeń. | Sprawdzone w Core, także przy różnym grupowaniu ticków. |
| Trasy | Siatka 14 × 9, deterministyczny BFS, zapis konkretnych pól trasy. | Replay sprawdzony. Nie ma fizycznych kolizji jednostek. |
| Produkcja | Podajnik → prasa → wysyłka; prasa automatycznie pracuje z zapasu. | Ręczna linia i replay sprawdzone w Core. |
| Magazyny | Osobne wejście i wyjście prasy; rezerwacja miejsca na gotowy produkt. | Pełne wyjście zatrzymuje nowy cykl bez utraty rudy; sprawdzone w Core. |
| Bufory | Osobny bufor rudy lub płyt z odkładaniem i odbieraniem. | Transfer przez bufor sprawdzony w Core. |
| Wspólne zasoby | Atomowy transfer; jedna obsługa naraz; kolejność wykonalnych zgłoszeń i rozstrzyganie remisów. | Test wspólnej płyty, priorytetu Echo i błędu konfliktu przeszedł. Pełne zachowanie kolejek do testów w grze. |
| Nagrywanie | Komendy ruchu, transferu, ilości, czasu i profilu. Do 5 aktywnych Echo. | Replay sprawdzony w Core. |
| Zarządzanie Echo | Nazwa przy zapisie, włączanie, wyłączanie, zastępowanie i odrzucenie próby. | Test wyłączonego Echo i zachowania starego nagrania po odrzuceniu zastępstwa przeszedł. |
| Profile | Stare Echo zachowują profil; istnieje jawne zastosowanie nowego profilu. | Brak automatycznej zmiany starej szybkości sprawdzony. Użyteczność aktualizacji do testu. |
| Planowanie | Przenoszenie stacji, zakup prasy i buforów, kontrola dojścia, cofanie zmian przed startem. | Wybrane przypadki zmiany układu i cofania sprawdzone w Core; sterowanie do testu w Unity. |
| Ulepszenia | Pojemności wejścia/wyjścia, produkcja, udźwig, ruch i obsługa. | Wybrane scenariusze sprawdzone w Core; wszystkie zakupy i limity wymagają sesji w Unity. |
| Rozliczenia | Credits za pełną zmianę; odrzucenie zachowuje zarobek, przerwanie nie płaci. | Testy wypłat i ochrony przed powtórnym rozliczeniem przeszły. |
| Autonomia | Dwa pełne przebiegi bez Operatora, porównanie stanu i zdarzeń, unieważnianie certyfikatu. | Sprawdzone w Core. Certyfikat jest tylko w pamięci sesji. |
| Autoloop | Powtarzanie symulacji i wypłata po pełnych zmianach. | Sprawdzone w Core. Brak naliczania offline. |

### Warstwa Unity i pliki projektu

| Element | Stan |
| --- | --- |
| Scena, `.meta`, wersja edytora i manifest pakietów | Przygotowane; sprawdzono JSON, unikalność GUID oraz powiązania sceny. Import Unity niewykonany. |
| Tymczasowa hala IMGUI | Kod planszy, stacji, Operatora, Echo, paneli i podsumowania istnieje. Wygląd i obsługa nieweryfikowane w Unity. |
| Sterowanie | Kod myszy, WASD/strzałek, E, 1/2/3, pauzy i tempa 0,5× istnieje. Do testu. |
| Dziennik | Zdarzenia i komunikaty błędów istnieją. Pełnej osi czasu jeszcze nie ma. |
| Zapis lokalny | Adapter JSON, plik tymczasowy, atomowe zastąpienie, kopia `.bak`, walidacja i odzyskanie. Działanie na docelowym systemie do sprawdzenia. |
| Menu edytora | Otwarcie hali, uruchomienie testów i budowanie Windows przygotowane. Build nie został wykonany. |
| Dokumentacja | README, DESIGN, PLAYTEST, VALIDATION i niniejszy STATUS. |
| Repozytorium | Projekt wysłany na `main`. Wcześniejsza blokada zapisu GitHub została rozwiązana. |

## Najbliższe zadania

Proponowany podział: Filip uruchamia Unity i przekazuje obserwacje; asystent przygotowuje poprawki kodu i testy; decyzje o rozgrywce podejmujemy wspólnie. Właściciela można zmienić przy rozpoczęciu zadania. Wszystkie zadania poniżej są obecnie **do zrobienia**.

| ID | Priorytet | Zadanie | Proponowany wykonawca | Warunek ukończenia |
| --- | --- | --- | --- | --- |
| U01 | Teraz | Zainstalować przypięte Unity, pobrać repo i zaimportować projekt. | Filip | Zapisany wynik importu oraz ewentualne błędy Console. |
| U02 | Teraz | Naprawić błędy importu i kompilacji Runtime/Editor, jeśli się pojawią. | Asystent + Filip | Projekt kompiluje się i wchodzi w Play. Jeśli brak błędów, odnotować to. |
| U03 | Teraz | Sprawdzić halę i pełny ręczny cykl produkcji. | Filip | Operator pobiera rudę, ładuje prasę, odbiera i wysyła płytę; UI jest używalne. |
| U04 | Teraz | Uruchomić testy logiki z menu Unity. | Filip | Wynik Console wraz z wersją Unity i commitem zapisany w VALIDATION. |
| U05 | Następne | Przejść checklistę PLAYTEST, w tym dwa współpracujące Echo. | Wspólnie | Wyniki każdego punktu i lista odtwarzalnych problemów. |
| U06 | Następne | Sprawdzić zapis, ponowne uruchomienie, kopię zapasową i przerwanie zmiany. | Wspólnie | Postęp nie ginie i nie jest wypłacany podwójnie; odzyskanie działa. Test uszkodzenia na kopii zapisu. |
| U07 | Następne | Poprawić największe problemy sterowania i czytelności z pierwszej sesji. | Asystent + Filip | Każdy zgłoszony problem ma poprawkę i ponowny test. |
| U08 | Następne | Zbudować i uruchomić prototyp Windows poza edytorem. | Filip / środowisko z Unity | Aplikacja startuje, realizuje cykl i zachowuje zapis. |
| U09 | Po pierwszej sesji | Sprawdzić ekonomię, pojemności i tempo; poprawić balans. | Wspólnie | Zakupy rozwiązują zauważalne problemy; podstawowa linia nie wymaga czekania offline. |
| U10 | Po pierwszej sesji | Dodać kolejne testy dla wykrytych ryzyk, np. ilości >1 i dłuższych kolejek. | Asystent | Test pokazuje konkretny problem i przechodzi po poprawce. |

**Bramka przejścia dalej:** hala uruchamia się w Unity; da się ręcznie produkować, zapisać Echo i uzyskać współpracę dostawcy z odbiorcą; magazyny i rozliczenia działają; nie ma znanego błędu utraty postępu. Wynik musi pochodzić z testu, nie z samej obecności kodu.

## Kolejne etapy po działającej hali

To kolejność robocza, nie deklaracja wykonania ani terminów.

| Etap | Do wykonania | Co ma potwierdzić zakończenie |
| --- | --- | --- |
| Czytelność i diagnoza | Oś czasu, podświetlanie błędu i stacji, wskazanie pierwszej przyczyny, widoczne oczekiwanie i zależne trasy. | Gracz rozumie, dlaczego Echo nie wykonało zadania i umie to poprawić. |
| Stabilna baza | Wersjonowane dane balansu, trwała identyfikacja konfiguracji/certyfikatu, rozwój zapisu i migracji, testy uruchamiane przy zmianach kodu. | Aktualizacja reguł nie zachowuje nieważnej autonomii ani nie niszczy zgodnych zapisów. |
| Oprawa i wejście | Docelowy UI i URP 2D, Input System, skalowanie, audio, dostępność; później kontroler. | Czytelna hala i wygodne sterowanie na sprzęcie docelowym. |
| Pierwszy sektor | Tutorial prowadzony czynnościami, kontrakty, Phase Shift, drugi etap produkcji i dopracowany cel autonomii. | Gracz samodzielnie przechodzi pełną pętlę sektora. |
| Meta i kampania | Seal Timeline, Memory, Rezerwa, archiwum fabryk, ograniczone wsparcie, rozliczenia offline i kolejne sektory. | Ukończona fabryka wspiera postęp zgodnie z dokumentem projektu; wypłaty są jednokrotne. |
| Wydanie | Steam, Cloud, lokalizacja, profilowanie, demo, testy urządzeń i ukończenie przyjętej kampanii. | Osobne kryteria demo i wydania; nie utożsamiamy ich z prototypem. |

Pełny plan zakłada do sześciu sektorów, z wariantem ograniczonym do czterech. Nie zobowiązujemy się teraz do całego rozszerzonego zakresu. Bramki z dokumentu projektu, w tym test z pięcioma osobami (co najmniej 3 chcą kolejnej zmiany, co najmniej 4 rozumieją mechanikę do 3 minut), **nie zostały jeszcze przeprowadzone**.

## Decyzje, których nie należy zgubić

- Magazyny i organizacja transportu są częścią rdzenia gry.
- Echo przenikają się na trasie; obsługa przy stacji jest ograniczona.
- Drogi są pomysłem do eksperymentu. Obowiązek chodzenia po drogach nie został przyjęty.
- Fizyczne kolizje poruszających się Echo są odłożone.
- Dodatkowe miejsca obsługi jako ulepszenie są propozycją do testu, nie gotową funkcją.
- Przeniesienie stacji nie naprawia automatycznie nagranej trasy.
- Przyspieszenie Operatora nie zmienia po cichu starych Echo.
- Parametry obecnego prototypu są testowe. Nie uznajemy ich za finalny balans ani wynik badań graczy.
- Sama instalacja nowych pakietów lub docelowej grafiki nie jest pilniejsza niż sprawdzenie pętli rozgrywki.

## Znane ograniczenia i otwarte problemy

| ID | Rodzaj | Stan na dziś | Dalsza praca |
| --- | --- | --- | --- |
| O01 | Brak weryfikacji | Projekt nie został uruchomiony w Unity. Nie znamy jeszcze błędów importu ani widoku. | U01–U05. |
| O02 | Ograniczenie | IMGUI i schematyczna plansza są tymczasowe; brak docelowego UI i grafiki. | Najpierw używalność, później oprawa. |
| O03 | Ograniczenie | Licznik zależnych nagrań uwzględnia wskazania stacji, nie wszystkie trasy przez nowe przeszkody. | Analiza tras i ostrzeżenie przed zmianą układu. |
| O04 | Ograniczenie | Certyfikat tylko w pamięci; po ponownym uruchomieniu potrzebny test. | Trwały certyfikat związany z danymi i wersją reguł. |
| O05 | Ograniczenie | Jeden slot obsługi, brak dróg, obracania i usuwania budynków. | Zakres rozszerzać po osobnej decyzji. |
| O06 | Ograniczenie | Dziennik zastępuje timeline; brak grafu przyczyn błędów. | Etap diagnozy. |
| O07 | Brak weryfikacji | Adapter plików i odzyskiwanie nie były sprawdzane w Unity/Windows. | U06; nie oznaczać zapisu jako w pełni przetestowanego. |

**Potwierdzone błędy z sesji w Unity:** jeszcze nie zgłoszono; brak sesji nie oznacza braku błędów.

### Szablon zgłoszenia błędu

Skopiuj poniższy blok do nowego wpisu albo issue; nadaj kolejny numer B01, B02 itd.

```text
ID / tytuł:
Status: nowe / w trakcie / poprawione, do testu / potwierdzone
Wersja Unity i commit:
Zgłasza / naprawia:
Kroki odtworzenia:
Oczekiwany wynik:
Rzeczywisty wynik:
Komunikat Console / screenshot:
Commit poprawki:
Wynik ponownego testu i data:
```

## Jak pracujemy razem

1. Na początku sesji czytamy ten plik i bieżący stan repozytorium. Sprawdzamy, czy ktoś nie zmienił kodu od ostatniego opisu.
2. Wybieramy konkretne zadanie po ID. Wpisujemy wykonawcę i status „w trakcie”; zmiana wykonawcy nie zmienia uzgodnionego celu.
3. Filip może zgłaszać problem zwykłym opisem, screenshotem lub błędem Console. Asystent przekłada to na odtwarzalne zadanie i poprawkę.
4. Po pracy zapisujemy commit, co zmieniono, co rzeczywiście sprawdzono oraz co pozostaje niesprawdzone. Nie odhaczamy testów za samo napisanie kodu.
5. Wyniki techniczne trafiają do VALIDATION, zmiany reguł do DESIGN, a bieżący postęp i kolejny krok tutaj. W przypadku sprzeczności opis trzeba uzgodnić i zaktualizować.
6. Nowe pomysły zapisujemy jako „do decyzji”, zamiast automatycznie powiększać zakres.

## Historia i przekazanie następnej sesji

| Data | Zdarzenie | Wynik |
| --- | --- | --- |
| 09.09.2026 | Przygotowanie pierwszego projektu Unity/C#. | Hala, bufory, planowanie, ulepszenia i Echo zapisane w kodzie. |
| 09.09.2026 | Testy rdzenia poza Unity. | 2079 asercji i 1000 powtórek przeszło; szczegóły w VALIDATION. |
| 09.09.2026 | Rozwiązanie problemu dostępu GitHub i wysłanie projektu. | Kod na `main`, commit `90199a5`. |
| 09.09.2026 | Dodanie wspólnego pliku statusu. | Następny krok: U01, pierwszy import Unity. |

**Na następną sesję:** sprawdzić U01. Jeśli Unity nadal nie jest dostępne, można pracować nad konkretnymi testami Core, ale weryfikacja sceny pozostaje otwarta. Jeśli import już wykonano, zacząć od komunikatów Console i wyniku uruchomienia hali.

Szablon kolejnego przekazania:

```text
Data / commit:
Zadanie:
Zmieniono:
Sprawdzono (środowisko i wynik):
Niesprawdzone / blokady:
Decyzje z tej sesji:
Następne zadanie:
```
