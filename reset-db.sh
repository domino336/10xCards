#!/bin/bash
# Reset Database Script dla Linux/Mac

echo "???  Usuwanie starej bazy danych..."
rm -f 10xCards/10xCards.db
rm -f 10xCards/10xCards.db-shm
rm -f 10xCards/10xCards.db-wal

echo "?? Tworzenie nowej migracji..."
dotnet ef migrations add InitialCreate --project 10xCards.Persistance --startup-project 10xCards

echo "?? Stosowanie migracji..."
dotnet ef database update --project 10xCards.Persistance --startup-project 10xCards

echo "? Gotowe! Mo¿esz uruchomiæ aplikacjê: dotnet run --project 10xCards"
