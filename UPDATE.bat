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

echo Sprawdzanie zmian na GitHubie...
echo.
git pull

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
