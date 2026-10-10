# Stan weryfikacji

## Wykonane 9 września 2026

Rdzeń Core i wspólne testy zostały skompilowane przez Roslyn C# z .NET SDK 8.0.408, w trybie zgodności języka C# 9. Zestaw uruchomiono na .NET 8.0.15 poza Unity.

Wynik początkowy: **2079 asercji przeszło**, w tym **1000 identycznych powtórek** ze zmiennym grupowaniem ticków. Porównywane są ślady stanów i dokładne zdarzenia.

Zakres: ręczna linia produkcyjna, replay, zapasy wejścia i wyjścia, rezerwacja produktu, pełny magazyn, atomowy odbiór wspólnej sztuki, priorytet Echo, jawny błąd konfliktu, wyłączone Echo, autonomia bez wypłaty, Autoloop, cofanie zakupów, niezmienność dawnych profili, zmiana układu bez cichej zmiany tras, zastępowanie i odrzucanie, brak podwójnej wypłaty, restart bez wypłaty, bufor, granica zmiany i zachowanie historii przy odrzuconej późnej komendzie.

Standardowy proces MSBuild nie mógł ukończyć uruchomienia w tym środowisku. Kompilacja testów została wykonana bezpośrednim wywołaniem lokalnego kompilatora Roslyn z lokalnymi referencjami .NET. Nie zastąpiono logiki testowanej kodem w innym języku. Skrypt projektu pozostaje standardowym projektem .NET do uruchomienia lokalnie.

## Druga sesja — planowanie, partie i kolejki (9 września 2026)

**2125 asercji przeszło**, w tym dotychczasowe 1000 deterministycznych powtórek. Wykonano kompilację Core i CoreChecks bezpośrednio przez Roslyn z SDK 8.0.408, C# 9, oraz uruchomienie na .NET 8.0.15.

Dodano 46 asercji obejmujących:

- Wykrycie przecięcia nagranej trasy przez nowy budynek, zmianę celu obsługi oraz uwzględnienie wyłączonych Echo bez duplikowania ID.
- Brak zmian salda, układu, ID, certyfikatu i historii cofania podczas podglądu; odrzucenie niedozwolonego pola; brak skutków kliknięcia bieżącej pozycji.
- Faktyczne przerwanie replay przez przewidzianą przeszkodę oraz odzyskanie autonomii i Credits po cofnięciu.
- Transport trzech sztuk przez bufor, odbiór dwóch z pozostawieniem reszty, produkcję, wysyłkę i replay partii.
- Obsługę trzech Echo według czasu przybycia, nawet gdy ich ID sugerują inną kolejność.
- Brak częściowego transferu i blokowania stanowiska przez niewykonalną partię, obsługę kolejnego wykonalnego żądania i wygaśnięcie oczekiwania.
- Współpracę nagranego dostawcy trzech rud i odbiorcy dwóch płyt, potwierdzoną testem autonomii.

Test partii uwzględnia czas na wyprodukowanie obu płyt; nie wydłużono okna komend ani nie zmieniono reguł symulacji. Kod podglądu kolorów i ostrzeżeń w Runtime **nie został skompilowany ani obejrzany w Unity**. Wynik testów dotyczy logiki Core.

## Trzecia sesja — diagnostyka (10 września 2026)

**2139 asercji przeszło** przy tym samym sposobie kompilacji: Roslyn z SDK 8.0.408, C# 9, uruchomienie na .NET 8.0.15 poza Unity. 1000 powtórek porównuje teraz także numer komendy i zapisane pole zdarzenia.

14 nowych asercji sprawdza: brak przedwczesnego zgłoszenia problemu, czas i numer pierwszej błędnej komendy, zachowanie pola zablokowanej trasy po dalszym ruchu, kontekst braku materiału w wyniku autonomii, rozpoznanie niedokończonej czynności na końcu zmiany, pierwszeństwo wcześniejszego błędu oraz brak mutacji zdarzeń i śladu przez odczyt diagnostyki. Pusta hala nadal nie otrzymuje certyfikatu tylko dlatego, że nie ma błędów jednostek.

Opis niedokończonej czynności jest informacją diagnostyczną; nie zwiększa licznika błędów i nie dodaje zdarzenia do symulacji. Nie zmieniono reguł ani schematu zapisu kampanii. Pierwszy zaobserwowany błąd nie jest analizą pełnego łańcucha przyczyn.

Filtr dziennika, ramka pola problemu, przejście do stacji i widok kolejki mają przygotowany kod Runtime. **Nie były kompilowane ani oglądane w Unity.**

## Jeszcze niewykonane

- Import sceny i ustawień przez Unity.
- Kompilacja assembly Runtime i Editor przeciw rzeczywistym bibliotekom Unity.
- Wizualna kontrola IMGUI i sterowania na laptopie.
- Sprawdzenie File.Replace i odzyskiwania lokalnego zapisu na docelowym systemie.
- Build Windows, test kontrolera, Steam Deck i profilowanie.
- Zewnętrzne sesje testerów ani bramki akceptacyjne dokumentu projektu.

Przejście testów Core nie jest potwierdzeniem działania projektu w Unity. Scena jest przygotowana do importu; pierwsze uruchomienie w edytorze pozostaje wymaganym krokiem.

## A09 — balans (9 października 2026)

Testy w `Tests/BalanceSimulation.cs` (uruchamiane z `StrategyChecks`) grają cztery skryptowane strategie przez 30 tur na 6 światach (seedy 1–6); drugi przebieg po 60 tur sprawdza strategię ekspansji. Zmiany liczb: cena bazowa produktów 120 → 260 C, powrót podaży rynku 1 → 3 poziomy na turę, koszt utrzymania 150 + 50/hala → 100 + 100/hala (start bez zmian: 200 C).

Wyniki po 30 turach (saldo końcowe, start 10 000 C; `ECHO_BALANCE_REPORT=1 dotnet run --project Tests/CoreRunner` wypisuje całą tabelę):

| Strategia | Wynik 30 tur | Uwagi |
| --- | --- | --- |
| Bierny (tylko koniec tury, sprzedaż produktów) | 20 047 C | start nie bankrutuje bez błędów gracza |
| Prasy z rynku (2 prasy, zakup stali i energii) | 24 789 C | najlepsza z badanych, minimum 8 900 C |
| Ekspansja (nowa działka + hala + 6 pras) | 5 226 – 24 789 C | zależy od losowych działek; po 60 turach wszystkie powyżej 10 000 C |
| Lekkomyślny (hale bez produkcji) | 2 300 C | wyraźnie gorszy od biernego |

Bankructwo: scenariusz z 4 dodatkowymi halami bez produkcji i saldem 1 000 C kończy się porażką w ≤ 12 turach (test).

Wnioski i ograniczenia: ekspansja nie wygrywa z prasami z rynku w 30 turach ani po 60 (zwraca się wolno; hala 6 000 C bez własnego dochodu), a cel 50 000 C nie jest osiągany przez żadną strategię w 60 turach (najlepsza ok. 41 000 C). Gracze ręczni mogą być lepsi (badania, ulepszone prasy, elektronika nie były w skryptach). Strategie są proste, nie dowód optymalności. Liczby nie były grane w Unity.

## A12 — balans celu i ekspansji (10 października 2026)

Zmiany liczb: cel 50 000 → 35 000 C, Hala Produkcyjna 6 000 → 4 000 C, Hala Badawcza 7 500 → 4 000 C, koszty badań 2 500 + 1 000·i → 1 500 + 500·i (1 500 … 4 000 C). Do symulacji dodano strategię „Badania” (Hala Badawcza, Automatyzacja, Ulepszona prasa, Efektywność energii, ulepszone prasy). Uruchomienie: `ECHO_BALANCE_REPORT=1 dotnet run --project Tests/CoreRunner`.

Wygrana (saldo ≥ 35 000 C), seedy 1–6, do 100 tur:

| Strategia | Tura wygranej |
| --- | --- |
| Prasy z rynku | 49 (każdy seed) |
| Ekspansja | 49 – 77 (zależnie od działek) |
| Badania | 85 |

Wnioski i ograniczenia: najlepsza strategia wygrywa w 40–60 turach (test), pozostałe wygrywają później. Ekspansja i badania nadal są wolniejsze od pras z rynku — na starcie hala ma tylko 2 miejsca, więc badania dają jedną ulepszoną prasę; tylko wystarczająco tanie, by nie były pułapką. Nie ma strategii dominującej w sensie dowodu, a skrypty są proste. Liczby nie były grane w Unity.

## A13 — szybsza ścieżka rozwoju (10 października 2026)

Zmiany: technologia „Automatyzacja podstawowa” daje +1 miejsce na maszynę w każdej Hali Produkcyjnej (`ProductionSystem.SlotCount`, startowa hala: 2 → 3), Hala Badawcza 4 000 → 1 000 C. Strategia „Badania” w symulacji kupuje teraz Hala Badawcza + Automatyzację i stawia zwykłe prasy w trzecim miejscu (ulepszone prasy i 4 miejsca wypadały gorzej — rynek się nasyca, a ulepszona prasa kosztuje 3 500 C).

Wygrana (saldo ≥ 35 000 C), seedy 1–6: Badania 49 tur (seed 3: 51), prasy z rynku 49, ekspansja 49–77. Test wymaga, by badania wygrywały nie później niż 3 tury po prasach z rynku. Liczby nie były grane w Unity.
