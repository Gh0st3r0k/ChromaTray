---
title: Začínáme
---

# Začínáme

## Spuštění aplikace

Použijte sestavení pro Windows x64 z důvěryhodného zdroje nebo projekt sestavte místně. Samostatné sestavení obsahuje .NET; odlehčené sestavení vyžaduje .NET 8 Desktop Runtime x64.

Pro spuštění ze zdrojového kódu ve Windows nainstalujte .NET 8 SDK a z kořenové složky repozitáře spusťte:

```powershell
dotnet restore ChromaTray.csproj --locked-mode
dotnet run --project ChromaTray.csproj
```

## Výběr barev

| Akce | Ovládání |
| --- | --- |
| Vybrat bod | `Ctrl+Shift+X` nebo **Pick Point** |
| Posunout výběr | Šipky, vždy o jeden fyzický pixel |
| Potvrdit bod | `Enter` nebo levé tlačítko myši |
| Skenovat oblast | `Ctrl+Shift+Z` nebo **Scan Region**, poté tažení myší |
| Zrušit | `Esc` nebo pravé tlačítko myši |
| Kopírovat vybranou barvu | Dvojité kliknutí nebo `Ctrl+C` |
| Změnit zkratky a formáty | **Settings** |
| Úplně ukončit aplikaci | Nabídka v oznamovací oblasti → **Exit** |

Aplikace před pořízením snímku pracovní plochy skryje své okno. Výběr bodu vždy vrátí původní RGB pixel. ClearType a další vyhlazování mohou vytvářet barevné pixely na okrajích textu; tyto pixely už existují na obrazovce a zůstávají viditelné v lupě.

## Prohlížení a export

Výběrem řádku palety aktualizujete informace o barvě. V úzkém okně použijte karty **Palette** a **Inspector**. Výběr exportu podporuje aktivní formáty, hlavní formát, vlastní proměnné CSS a JSON.

Přepínač **Group similar shades** je pod nadpisem palety a také v nastavení. Ve výchozím stavu je vypnutý. Po zapnutí každou RGB skupinu zastupuje její nejčastější původní barva; počet pixelů zahrnuje všechny odstíny ve skupině. Seznam zobrazuje nejvýše 500 položek a procenta se počítají z celé zachycené oblasti.
