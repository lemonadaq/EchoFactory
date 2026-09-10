# Pierwsza sesja w Unity

Status: do wykonania w edytorze Unity 6000.3.18f1.

1. Import projektu kończy się bez błędów w Console. Otwórz Factory.unity i Play. Powinna pojawić się hala z trzema stacjami i panelem po prawej.
2. Wykonaj 1 → 2, poczekaj na płytę, 2 → 3. Każdy przycisk naciskaj po zakończeniu poprzedniej czynności. Sprawdź stan magazynów i wynik.
3. Zakończ zmianę, zapisz Echo. Następna zmiana powinna odtworzyć zadanie bez sterowania Operatorem.
4. Nagraj osobno dostawcę: kilka razy 1 → 2. Z jego udziałem nagraj odbiorcę: 2 → 3. Sprawdź współpracę i test autonomii.
5. Kup udźwig, ustaw ilość 2 i przetestuj transfer. Stare Echo powinno nadal mieć udźwig 1. Odbiór dwóch płyt zleć z uwzględnieniem dwóch cykli produkcji; zbyt wczesna komenda może przekroczyć 6 sekund oczekiwania. Sprawdź, że niedostępna partia nie jest pobierana częściowo.
6. Postaw bufor, dostarcz i odbierz materiał przez panel Stacja. Zmiana powinna resetować jego zapas.
7. Przenieś prasę w planowaniu. Sprawdź komunikat z ID nagrań, brak certyfikatu i błędy starych tras. Postaw również bufor na zapisanej trasie Echo, które nie obsługuje tego bufora: podgląd powinien wskazać zagrożone nagranie przed kliknięciem. Sprawdź czerwone pole niedozwolone, bursztynowe pole z ostrzeżeniem oraz zielone bez bezpośredniej kolizji. Sam ruch myszy nie wydaje Credits. Powtórz z wyłączonym Echo. Cofnij zmianę przed startem; układ i saldo powinny wrócić. Kliknięcie dotychczasowej pozycji budynku nie powinno kasować certyfikatu.
8. Wypełnij wyjście prasy. Produkcja musi się zatrzymać bez straty materiału; odbiór ma ją wznowić.
9. Zapisz, wyłącz Play, uruchom ponownie. Credits, układ, ulepszenia i Echo powinny się zachować; produkcja zaczyna od stanu bazowego.
10. Przerwij zmianę z wysłaną płytą. Nie powinna dać wypłaty. Zakończ inną i odrzuć nagranie — zarobek powinien pozostać.
11. Zmień fokus, pauzuj i użyj tempa 0,5×. Po wznowieniu sprawdź brak skoku czasu i podwójnej wypłaty.
12. Sprawdź dwa pełne cykle Autoloop, wyłącz go przez Przerwij i potwierdź saldo.

## Diagnostyka — dodatkowy test po pierwszym uruchomieniu

1. Z pustej prasy zleć odbiór płyty i poczekaj na upływ okna komendy. W Dzienniku powinien pojawić się pierwszy problem: czas, Operator, numer komendy, stacja #2 i brak materiału. Sprawdź filtr „Tylko błędy” oraz „Przejdź do stacji”.
2. Zapisz poprawną trasę Echo, następnie postaw budynek na jej polu i sprawdź autonomię. Dziennik ma wyróżniać czerwonym obramowaniem problematyczne pole, także jeżeli Echo później przemieściło się dalej.
3. W trakcie pracy dwóch Echo wybierz wspólną stację. Panel powinien pokazać jednostkę w obsłudze i oczekujących z aktualnym powodem. Samo czekanie na materiał nie powinno być od razu błędem.
4. Jeśli zmiana kończy się w trakcie czynności, Dziennik powinien ją opisać nawet przy zerowym liczniku błędów. Brak błędów w pustej hali nie oznacza udanego testu autonomii.

Raport problemu: wersja Unity, kroki, oczekiwany efekt, rzeczywisty efekt, komunikat Console i opcjonalnie screenshot. Nie wysyłaj całego folderu Library/.
