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

## Jeszcze niewykonane

- Import sceny i ustawień przez Unity.
- Kompilacja assembly Runtime i Editor przeciw rzeczywistym bibliotekom Unity.
- Wizualna kontrola IMGUI i sterowania na laptopie.
- Sprawdzenie File.Replace i odzyskiwania lokalnego zapisu na docelowym systemie.
- Build Windows, test kontrolera, Steam Deck i profilowanie.
- Zewnętrzne sesje testerów ani bramki akceptacyjne dokumentu projektu.

Przejście testów Core nie jest potwierdzeniem działania projektu w Unity. Scena jest przygotowana do importu; pierwsze uruchomienie w edytorze pozostaje wymaganym krokiem.
