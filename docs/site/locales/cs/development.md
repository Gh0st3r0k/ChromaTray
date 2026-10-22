---
title: Vývoj
---

# Vývoj

## Orientace v kódu

| Komponenta | Úloha |
| --- | --- |
| `Program.cs` | Inicializace WinForms, kontrola jediné spuštěné instance a otevření hlavního okna. |
| `MainDashboardForm.cs` | Životní cyklus okna, příkazy oznamovací oblasti, nastavení a klávesové zkratky. |
| `MainDashboardForm.Layout.cs` | Vytvoření ovládacích prvků a přizpůsobení rozložení velikosti okna a DPI. |
| `MainDashboardForm.Capture.cs` | Skrytí aplikace, zachycení obrazovky a analýza palety s možností zrušení. |
| `MainDashboardForm.Palette.cs` | Výběr barvy, aktualizace inspektoru, kopírování a export. |
| `ScreenOverlayForm` | Vlastnictví snímku a výběr bodu nebo oblasti. Část `.Painting.cs` vykresluje překryv. |
| `Forms/Controls` | Vykreslování ovládacích prvků, virtuálních řádků palety a posuvníků. |
| `Services` | Příprava snímku, histogram, export, validace a globální zkratky Windows. |
| `Models` | Nastavení, klávesové zkratky a reprezentace barev. |

Zachytávání používá fyzické souřadnice obrazovky, které mohou být na dalších monitorech záporné. Souřadnice snímku se počítají od levého horního rohu virtuální pracovní plochy. Při úpravách výběru pixelů tyto soustavy souřadnic rozlišujte.

Překryv vlastní snímek a oříznutý obrázek. Hlavní okno kopíruje zachycenou oblast pro pozdější analýzu; každá úloha na pozadí dostane další vlastní kopii. Číslo požadavku brání tomu, aby starší výsledek přepsal novější výběr.

## Sestavení a testování

```powershell
dotnet restore ChromaTray.Tests/ChromaTray.Tests.csproj --locked-mode
dotnet test ChromaTray.Tests/ChromaTray.Tests.csproj -c Release --no-restore
```

Úplná sada testů vyžaduje Windows. Na Linuxu/macOS spusťte `ChromaTray.Core.Tests/ChromaTray.Core.Tests.csproj`, který testuje společné algoritmy barev, histogramu, navigace a exportu. Linux dokáže i zkompilovat projekt pro Windows díky nastavení `EnableWindowsTargeting`.

## Sestavení dokumentace

Nainstalujte .NET 8 SDK a Python 3. Z kořenové složky repozitáře spusťte:

```powershell
dotnet tool restore
dotnet restore ChromaTray.csproj --locked-mode
python scripts/build-docs.py
```

Na Linuxu/macOS podle potřeby použijte `python3` místo `python`. Hotový web se uloží do `artifacts/docs-site`. Místní náhled spustíte takto:

```powershell
dotnet docfx serve artifacts/docs-site --port 8080
```

Otevřete `http://localhost:8080/cs/`. Anglická verze je v kořeni webu; překlady jsou v `/sk/`, `/cs/`, `/ru/` a `/uk/`. Každý jazyk má vlastní vyhledávací index. Příručky upravujte v `docs/site/locales/<jazyk>` a texty rozhraní v souboru `template/token.json` ve stejné složce. Společná šablona přidává přepínač, který zachová aktuální stránku.

API metadata a HTML se generují automaticky a jsou vyloučena z Gitu. Verze DocFX je pevně určená v `.config/dotnet-tools.json`. Anglické popisy API aktualizujte úpravou komentářů `/// <summary>`, `<param>` a `<returns>` v C#. Běžné implementační komentáře `//` zůstávají ve zdrojovém kódu. Reference zobrazuje veřejné typy a členy; tato stránka vysvětluje i interní komponenty.

## Automatické aktualizace

Workflow **Documentation** sestaví všechny jazyky při změnách a pull requestech a uloží web do artefaktu `documentation-site`. Kontrola pull requestu web nepublikuje.

Pro automatické publikování přes GitHub Pages musí správce repozitáře jednorázově:

1. V **Settings → Pages** nastavit **Source: GitHub Actions**.
2. Otevřít **Settings → Secrets and variables → Actions → karta Variables → New repository variable**. Zadat název `DOCS_PUBLISH_ENABLED` a hodnotu `true`.
3. Spustit workflow **Documentation** nebo odeslat změnu dokumentace či kódu do `main`.

Po nastavení úspěšná sestavení větve `main` publikují web. Veřejná adresa se zobrazí v prostředí nasazení workflow. Bez tohoto nastavení zůstává celý web dostupný ke stažení jako artefakt.
