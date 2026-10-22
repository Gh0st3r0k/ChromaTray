---
title: Začíname
---

# Začíname

## Spustenie aplikácie

Použite zostavenie pre Windows x64 z dôveryhodného zdroja alebo si projekt zostavte lokálne. Samostatné zostavenie obsahuje .NET; odľahčené zostavenie vyžaduje .NET 8 Desktop Runtime x64.

Na spustenie zo zdrojového kódu vo Windows nainštalujte .NET 8 SDK a z koreňového priečinka repozitára spustite:

```powershell
dotnet restore ChromaTray.csproj --locked-mode
dotnet run --project ChromaTray.csproj
```

## Výber farieb

| Akcia | Ovládanie |
| --- | --- |
| Vybrať bod | `Ctrl+Shift+X` alebo **Pick Point** |
| Posunúť výber | Šípky, vždy o jeden fyzický pixel |
| Potvrdiť bod | `Enter` alebo ľavé tlačidlo myši |
| Skenovať oblasť | `Ctrl+Shift+Z` alebo **Scan Region**, potom potiahnutie myšou |
| Zrušiť | `Esc` alebo pravé tlačidlo myši |
| Kopírovať vybranú farbu | Dvojité kliknutie alebo `Ctrl+C` |
| Zmeniť skratky a formáty | **Settings** |
| Úplne ukončiť aplikáciu | Ponuka v oznamovacej oblasti → **Exit** |

Aplikácia pred vytvorením snímky pracovnej plochy skryje svoje okno. Výber bodu vždy vráti pôvodný RGB pixel. ClearType a iné vyhladzovanie môžu vytvárať farebné pixely na okrajoch textu; tieto pixely už existujú na obrazovke a zostávajú viditeľné v lupe.

## Prezeranie a export

Výberom riadka palety aktualizujete informácie o farbe. V úzkom okne použite karty **Palette** a **Inspector**. Výber exportu podporuje aktívne formáty, hlavný formát, vlastné premenné CSS a JSON.

Prepínač **Group similar shades** je pod nadpisom palety a tiež v nastaveniach. Predvolene je vypnutý. Keď je zapnutý, každú RGB skupinu zastupuje jej najčastejšia pôvodná farba; počet pixelov zahŕňa všetky odtiene v skupine. Zoznam zobrazuje najviac 500 položiek a percentá sa počítajú z celej zachytenej oblasti.
