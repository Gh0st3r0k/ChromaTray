---
title: Plány rozvoja
---

# Plány rozvoja

Nápady pre budúce verzie. Zoznam sa môže meniť a nepredstavuje záväzok k termínom.

## Témy vzhľadu

Pridať svetlú a tmavú tému a potom zvážiť vlastné farby rozhrania. Zachovať čitateľný text, prístupný kontrast a jednotný vzhľad okna, nastavení, ponúk a výberu farieb.

## Windows, Linux a macOS

Preskúmať náhradu Windows Forms multiplatformným rozhraním, napríklad Avalonia so zachovaním C#. Opätovne využiť farebné algoritmy, export a vhodné testy. Snímanie obrazovky, globálne klávesové skratky, systémová lišta a oprávnenia OS vyžadujú samostatné implementácie a overenie pre každú platformu.

## Možná zmena jazyka

Vyhodnotiť iné jazyky a technológie na malom prototype. O prepísaní rozhodnúť až po porovnaní rýchlosti spustenia a obnovenia okna, spotreby pamäte a náročnosti vývoja. Samotná zmena jazyka nezaručuje vyšší výkon ani podporu viacerých OS.

## Palety pre webový dizajn

Vytvoriť päť zladených farieb na základe vybranej farby, umožniť uzamknutie jednotlivých farieb a nové generovanie ostatných. Zobraziť výsledok na ukážkovom webe, overovať kontrast textu a tlačidiel a exportovať premenné CSS. Ide o celé dizajnové palety, nielen svetlejšie či tmavšie odtiene jednej farby. Pred výberom implementácie vyhodnotiť online generátory a otvorené alternatívy.

### Poskytovatelia paliet a osobné kľúče

Naplánovať nastavenie **Zdroj paliet**. Prvým kandidátom je Colormind: zverejnené príklady API nevyžadujú účet ani kľúč. Pri ďalších poskytovateľoch umožniť vlastný API kľúč, ak ho služba podporuje; požiadavky sa budú účtovať alebo obmedzovať podľa účtu a podmienok používateľa. Nevyžadovať kľúč pri službách, ktoré ho nepoužívajú.

Požiadavky odosielať iba na výslovný pokyn používateľa a prenášať vybrané farby RGB, nie snímky obrazovky. Osobné kľúče ukladať do zabezpečeného úložiska operačného systému namiesto bežného `settings.json`; umožniť vymazanie a vylúčiť kľúče z protokolov a exportu.

Zverejnené podmienky Colormind povoľujú bezplatné osobné a nekomerčné použitie API; pri komerčnej aplikácii treba kontaktovať vlastníka služby. Licencia MIT ChromaTray neposkytuje ďalšie práva k externým API. Tieto samostatné podmienky uviesť v dokumentácii. Pred integráciou overiť podporu HTTPS a aktuálne podmienky služby. Uzamknuté farby zachovať presne v aplikácii: Colormind môže vstupné farby mierne zmeniť.

Vetva `colormind-palettes` implementuje generátor Colormind. Výber medzi viacerými poskytovateľmi a ukladanie osobných kľúčov zostávajú v pláne.
