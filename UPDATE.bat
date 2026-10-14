@echo off
title EchoFactory - Update
cd /d "%~dp0"

echo ========================================
echo        EchoFactory - GitHub Update
echo ========================================
echo.
echo Folder projektu:
echo %CD%
echo.

where git >nul 2>&1
if errorlevel 1 (
    echo BLAD: Git nie jest zainstalowany lub nie ma go w PATH.
    echo Zainstaluj Git i uruchom UPDATE.bat ponownie.
    echo.
    pause
    exit /b 1
)

rem Lokalne zmiany (np. pliki skopiowane z ZIP albo przestawione przez Unity) blokuja git pull.
rem Odkladamy je do schowka git stash, zamiast kasowac - zawsze mozna je odzyskac.
set DIRTY=
for /f "delims=" %%i in ('git status --porcelain') do set DIRTY=1
if defined DIRTY (
    echo Znaleziono lokalne zmiany. Odkladam je do schowka ^(git stash^), nic nie jest kasowane.
    echo Zamknij Unity przed aktualizacja.
    git stash push --include-untracked -m "UPDATE.bat kopia %DATE% %TIME%"
    if errorlevel 1 (
        echo BLAD: nie udalo sie odlozyc lokalnych zmian.
        pause
        exit /b 1
    )
    echo Kopia zapisana. Lista kopii: git stash list
    echo.
)

echo Sprawdzanie zmian na GitHubie...
echo.
git pull --ff-only

if errorlevel 1 (
    echo.
    echo ========================================
    echo AKTUALIZACJA NIEUDANA
    echo ========================================
    echo.
    echo Sprawdz komunikat powyzej.
    echo Nie uruchamiaj Unity, dopoki problem nie zostanie rozwiazany.
    echo.
    pause
    exit /b 1
)

echo.
echo ========================================
echo AKTUALIZACJA ZAKONCZONA
echo ========================================
echo.
echo Projekt jest aktualny.
echo Mozesz teraz uruchomic Unity Hub i otworzyc projekt.
echo.
pause
