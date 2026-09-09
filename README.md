# Echo Factory — prototyp Unity

Pierwsza hala gry o automatyzacji przez nagrywanie własnej pracy. Projekt jest napisany w C#, z niezależną symulacją i tymczasowym interfejsem 2D w Unity.

**Edytor przypięty w projekcie: Unity 6.3 LTS, dokładnie `6000.3.18f1`.** [Strona tej wersji i instalatory](https://unity.com/releases/editor/whats-new/6000.3.18f1). Jest to wybrana, zweryfikowana dostępna wersja, nie deklaracja najnowszego patcha. Na tym etapie używamy wbudowanego renderera i IMGUI; docelowa oprawa URP 2D i Input System są odłożone do kolejnego etapu. Logika nie zależy od renderera.

## Uruchomienie po instalacji Unity

1. Pobierz repozytorium przez `git clone https://github.com/lemonadaq/EchoFactory.git` lub **Code → Download ZIP** i rozpakuj.
2. Zainstaluj Unity Hub, a w nim edytor **6000.3.18f1**. Do pracy w edytorze nie potrzebujesz teraz dodatkowych modułów mobilnych ani WebGL.
3. W Unity Hub wybierz **Add → Add project from disk** i wskaż folder zawierający `Assets`, `Packages` i `ProjectSettings`. Nie twórz nowego projektu wewnątrz tego folderu.
4. Otwórz projekt. Po imporcie wybierz **Echo Factory → Otwórz halę** albo otwórz `Assets/EchoFactory/Scenes/Factory.unity`.
5. Wciśnij **Play**. W oknie Game ustaw proporcje zbliżone do **1440 × 930** i powiększ je w razie potrzeby. Interfejs jest tymczasową planszą schematyczną.

Projekt nie wymaga płatnych assetów. W repozytorium są stabilne pliki `.meta` i scena startowa; Unity uzupełni pozostałe domyślne ustawienia przy pierwszym imporcie.

## Jak grać

- Kliknij **Rozpocznij zmianę**.
- Klawisz **1**: podejdź do podajnika i pobierz rudę.
- **2** z rudą: podejdź do prasy i odłóż ją do magazynu wejściowego. Możesz od razu wrócić po kolejną dostawę.
- **2** z pustymi rękami: odbierz płytę z magazynu wyjściowego. Gdy jeszcze jej nie ma, Operator poczeka w oknie akcji.
- **3**: zanieś płytę do wysyłki.
- Po 60 sekundach wybierz **Zapisz Echo**. Kolejna zmiana powtórzy nagranie obok Operatora.

Myszą wybierasz stację, a w panelu **Stacja** wskazujesz odbiór lub odłożenie materiału. Akcja obejmuje dojście i obsługę. Po ulepszeniu udźwigu ustaw ilość przyciskami `−` / `+`. Ręce przyjmują jeden rodzaj materiału naraz, a transfer żądanej ilości jest atomowy.

WASD lub strzałki poruszają o jedno pole; **E** wykonuje domyślną akcję wybranej stacji; **Spacja** pauzuje; **Tab** zmienia panel; **Esc** anuluje ustawianie budynku. **Jak grać** otwiera instrukcję.

## Zakres tej wersji

- Jedna hala 14 × 9 pól, podajnik, prasa, wysyłka.
- Oddzielne wejście i wyjście prasy; automatyczna produkcja z zapasu i rezerwacja miejsca na gotowy produkt.
- Przenoszenie stacji podczas planowania, zakup dodatkowej prasy i buforów rudy lub płyt; sprawdzanie dostępności dojścia.
- Ulepszenia pojemności, czasu produkcji, udźwigu, ruchu i obsługi; cofanie zakupów/układu do startu zmiany.
- Do 5 aktywnych Echo, nagrywanie, wyłączanie, zastępowanie i jawne zastosowanie nowego profilu.
- Wspólne zasoby i jedna obsługa naraz przy stacji; przenikanie na trasie.
- Dziennik błędów, dwa deterministyczne testy autonomii bez Operatora i Autoloop aktywnej hali.
- Credits wypłacane za pełne zmiany, przerwanie bez częściowej wypłaty, lokalny zapis i kopia poprzedniej generacji.

## Ważne zasady

Początkowe **200 Credits**, ceny ulepszeń, pojemności **4 sztuki**, udźwig **1**, ruch **0,5 s/pole**, obsługa **1 s**, prasa **4 s** i minimalny cel autonomii **1 płyta/zmianę** są parametrami testowymi, a nie finalnym balansem. Linia bazowa działa bez zakupów. Podajnik startuje z 3 sztukami i uzupełnia jedną co 4 sekundy do swojej pojemności.

Każda zmiana resetuje zapasy, pozycje i pracę maszyn. Budynki, ulepszenia i Credits pozostają. Odrzucenie nagrania zachowuje zarobek; restart nie płaci za niepełną zmianę. Zamknięcie podczas pracy zachowuje ostatni stan trwały, ale nie nagranie ani częściową wypłatę.

Przeniesienie budynku **nie przelicza starych tras**. Ich czas i jawne pola pozostają zapisane. Zmiana układu, ulepszenie, przełączenie Echo lub aktualizacja profilu unieważnia certyfikat. Trzeba sprawdzić wynik i w razie potrzeby nagrać zadanie ponownie. Dawne Echo nie przejmują automatycznie nowych szybkości. Jawna zmiana profilu zachowuje terminy komend; nowe nagranie w pełni wykorzystuje krótsze czasy.

## Zapis

Plik `echo-factory-v1.json` jest przechowywany w `Application.persistentDataPath`. Dla Windows i ustawień tego projektu jest to zwykle `%USERPROFILE%\AppData\LocalLow\EchoFactory\EchoFactory`. Zapis tworzy plik tymczasowy i atomowo zastępuje poprzedni, zachowując `.bak`. Nieobsługiwana wersja lub uszkodzone dane blokują nadpisanie. Ekran błędu umożliwia odzyskanie poprawnej kopii, zachowując uszkodzony oryginał osobno.

Certyfikat wymaga nowego testu po uruchomieniu gry. Autoloop działa podczas aktywnej gry; utrata fokusu pauzuje. **Nie ma jeszcze produkcji offline ani synchronizacji między urządzeniami.** W razie błędu zapisu widoczny jest komunikat; pamięć sesji może zawierać nowszy postęp niż plik.

## Testy

**Wykonane w środowisku bez Unity:** kompilacja kodu Core i testów kompilatorem Roslyn z .NET 8, uruchomienie 1000 deterministycznych powtórek oraz testów magazynów, transferu, konfliktu, ulepszeń, zmiany układu, autonomii, zastępowania i wypłat. Wynik i ograniczenia: [docs/VALIDATION.md](docs/VALIDATION.md).

W Unity: **Echo Factory → Uruchom testy logiki**. Wynik pojawi się w Console, a nie w oknie Unity Test Runner. To ten sam zestaw testów, bez dodatkowych pakietów.

Opcjonalnie z .NET 8 SDK:

```sh
dotnet restore Tests/CoreRunner/CoreRunner.csproj --configfile Tests/CoreRunner/NuGet.Config -p:NuGetAudit=false
dotnet run --project Tests/CoreRunner/CoreRunner.csproj --configuration Release --no-restore
```

**Nie wykonano jeszcze:** importu projektu, kompilacji assembly Runtime/Editor w prawdziwym Unity, wizualnego testu sceny ani buildu Windows. Kod sceny wymaga tego sprawdzenia; przejście testów Core nie oznacza gotowości całej gry. Pierwszą sesję przeprowadź według [docs/PLAYTEST.md](docs/PLAYTEST.md).

## Struktura i kolejny krok

- `Assets/EchoFactory/Core`: dane, deterministyczna ścieżka BFS, tick 20 Hz, nagrania, ekonomia i planowanie.
- `Assets/EchoFactory/Runtime`: scena schematyczna, wejście przez IMGUI, adapter zapisu.
- `Assets/EchoFactory/Editor`: menu uruchomienia, testów i opcjonalnego buildu Windows.
- `Assets/EchoFactory/Tests`: wspólne testy C#, dostępne także poza edytorem.
- `docs/DESIGN.md`: przyjęte decyzje i doprecyzowania do tej implementacji.

Pierwszy krok po instalacji to import i test tej hali, następnie poprawki wynikające z gry. Drogi, dodatkowe miejsca obsługi, pełna oś czasu, kampania, Seal, Memory, Steam, kontroler i docelowa grafika są poza tym wydaniem.
