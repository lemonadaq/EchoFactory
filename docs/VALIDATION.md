# Stan weryfikacji

## Wykonane 9 września 2026

Rdzeń Core i wspólne testy zostały skompilowane przez Roslyn C# z .NET SDK 8.0.408, w trybie zgodności języka C# 9. Zestaw uruchomiono na .NET 8.0.15 poza Unity.

Wynik początkowy: **2079 asercji przeszło**, w tym **1000 identycznych powtórek** ze zmiennym grupowaniem ticków. Porównywane są ślady stanów i dokładne zdarzenia.

Zakres: ręczna linia produkcyjna, replay, zapasy wejścia i wyjścia, rezerwacja produktu, pełny magazyn, atomowy odbiór wspólnej sztuki, priorytet Echo, jawny błąd konfliktu, wyłączone Echo, autonomia bez wypłaty, Autoloop, cofanie zakupów, niezmienność dawnych profili, zmiana układu bez cichej zmiany tras, zastępowanie i odrzucanie, brak podwójnej wypłaty, restart bez wypłaty, bufor, granica zmiany i zachowanie historii przy odrzuconej późnej komendzie.

Standardowy proces MSBuild nie mógł ukończyć uruchomienia w tym środowisku. Kompilacja testów została wykonana bezpośrednim wywołaniem lokalnego kompilatora Roslyn z lokalnymi referencjami .NET. Nie zastąpiono logiki testowanej kodem w innym języku. Skrypt projektu pozostaje standardowym projektem .NET do uruchomienia lokalnie.

## Jeszcze niewykonane

- Import sceny i ustawień przez Unity.
- Kompilacja assembly Runtime i Editor przeciw rzeczywistym bibliotekom Unity.
- Wizualna kontrola IMGUI i sterowania na laptopie.
- Sprawdzenie File.Replace i odzyskiwania lokalnego zapisu na docelowym systemie.
- Build Windows, test kontrolera, Steam Deck i profilowanie.
- Zewnętrzne sesje testerów ani bramki akceptacyjne dokumentu projektu.

Przejście testów Core nie jest potwierdzeniem działania projektu w Unity. Scena jest przygotowana do importu; pierwsze uruchomienie w edytorze pozostaje wymaganym krokiem.
