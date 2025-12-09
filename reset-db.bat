@echo off
REM Reset Database Script dla Windows

echo Usuwanie starej bazy danych...
del /f /q 10xCards\10xCards.db 2>nul
del /f /q 10xCards\10xCards.db-shm 2>nul
del /f /q 10xCards\10xCards.db-wal 2>nul

echo Tworzenie nowej migracji...
dotnet ef migrations add InitialCreate --project 10xCards.Persistance --startup-project 10xCards

echo Stosowanie migracji...
dotnet ef database update --project 10xCards.Persistance --startup-project 10xCards

echo Gotowe! Mozesz uruchomic aplikacje: dotnet run --project 10xCards
pause
