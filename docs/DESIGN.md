# Ustalenia projektu

Podstawa: dokument projektu Echo Factory v0.2 i zaakceptowany aneks dotyczący Unity. Ten plik zawiera techniczne ustalenia potrzebne do pracy w repozytorium; nie publikuje całego dokumentu biznesowego.

## Przyjęte z rozmowy

- Dalsza produkcja w Unity/C#; przeglądarka była eksperymentem.
- Budynki produkcyjne mają rozwijane magazyny wejścia i wyjścia. Podajnik używa wyjścia, wysyłka odbiera gotowy produkt, bufor ma jeden współdzielony magazyn.
- Gracz ustawia stacje i bufory na siatce w planowaniu. Układ jest stały przez całą zmianę.
- Ulepszenia produkcji, pojemności i transportu. Profile dawnych Echo są zachowane.
- Jednostki przenikają się na trasie, ale stanowiska mają ograniczoną obsługę.
- Drogi są eksperymentem na później, kolizje ruchu poza pierwszym zakresem.

## Doprecyzowania implementacji do sprawdzenia w grze

1. Współrzędne mają Y rosnące w dół. Każda stacja zajmuje pole i ma punkt obsługi o jedno pole niżej. Układ musi zapewniać dojście ze spawnu do wszystkich punktów. Obracanie i usuwanie stacji są odłożone.
2. Ścieżki BFS są deterministyczne: stała kolejność sąsiadów góra, lewo, prawo, dół. Wszystkie pola mają równy koszt, więc BFS daje najkrótszą drogę. Zapis przechowuje konkretne pola, bez ponownego pathfindingu w replay.
3. Prasa zużywa rudę i rezerwuje miejsce na płytę na początku procesu. Pełne wyjście zatrzymuje start kolejnego cyklu, nie niszczy już wytwarzanego materiału.
4. Każda stacja ma jedno miejsce obsługi. Pierwsza wykonalna akcja według momentu dołączenia do kolejki otrzymuje obsługę. Remis rozstrzyga starsze Echo, Operator jest ostatni. Nie ma twardej rezerwacji miejsca przez jednostkę, której brakuje materiału; nie blokuje ona dostawcy, który może usunąć przyczynę oczekiwania.
5. Obsługa trwa czas z profilu, transfer następuje atomowo na jej końcu. Maszyna może w tym samym ticku uruchomić kolejny proces, ale nie wykonuje od razu części nowego cyklu.
6. Akcja może rozpocząć się najwcześniej w zapisanym ticku i najpóźniej 120 ticków później. Usługa rozpoczęta w oknie może je zakończyć po jego końcu, lecz nie po końcu zmiany. Oczekiwanie nie przesuwa terminów pozostałych komend.
7. Zmiana budynku unieważnia autonomię i pokazuje liczbę nagrań wskazujących tę stację. Inne trasy mogą również zostać zablokowane nową przeszkodą; obecny licznik nie analizuje wszystkich przecinających ją tras.
8. Certyfikat w pamięci jest związany z konfiguracją przez kontrolowane operacje sesji, które go kasują. Trwały hash konfiguracji i wersjonowane certyfikaty kampanii pozostają kolejnym etapem. Symulacja porównuje ślad stanu i dokładne zdarzenia dwóch pełnych przebiegów bez Operatora.
9. Autoloop symuluje kolejne rzeczywiste zmiany, wypłaca wyłącznie sprzedaż i nie nagrywa Operatora. Nie liczy czasu offline.
10. Rdzeń używa stałych testowych w klasie Rules i modelach. Eksport balansu do wersjonowanego JSON, Input System, URP 2D i docelowy UI będą dodane po walidacji tej hali.

## Kontrola zakresu

Pełny plan z aneksu realizujemy etapami. Ta wersja wprowadza praktyczną halę z budową i buforami; nie kończy kampanii ani całej bramki prototypu. Dziennik zastępuje tymczasowo szczegółową oś czasu. Sprzężone błędy nie mają jeszcze grafu przyczyn. Nie ma dodatkowych slotów obsługi, automatycznych korekt trasy, cięcia nagrań ani eksperymentu z drogami.
