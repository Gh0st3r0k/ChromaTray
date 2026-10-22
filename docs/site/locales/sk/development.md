---
title: Vývoj
---

# Vývoj

## Orientácia v kóde

| Komponent | Úloha |
| --- | --- |
| `Program.cs` | Inicializácia WinForms, kontrola jedinej spustenej inštancie a otvorenie hlavného okna. |
| `MainDashboardForm.cs` | Životný cyklus okna, príkazy oznamovacej oblasti, nastavenia a klávesové skratky. |
| `MainDashboardForm.Layout.cs` | Vytvorenie ovládacích prvkov a prispôsobenie rozloženia veľkosti okna a DPI. |
| `MainDashboardForm.Capture.cs` | Skrytie aplikácie, zachytenie obrazovky a analýza palety s možnosťou zrušenia. |
| `MainDashboardForm.Palette.cs` | Výber farby, aktualizácia inšpektora, kopírovanie a export. |
| `ScreenOverlayForm` | Vlastníctvo snímky a výber bodu alebo oblasti. Časť `.Painting.cs` vykresľuje prekrytie. |
| `Forms/Controls` | Vykresľovanie ovládacích prvkov, virtuálnych riadkov palety a posuvníkov. |
| `Services` | Príprava snímky, histogram, export, validácia a globálne skratky Windows. |
| `Models` | Nastavenia, klávesové skratky a reprezentácie farieb. |

Zachytávanie používa fyzické súradnice obrazovky, ktoré môžu byť na ďalších monitoroch záporné. Súradnice snímky sa počítajú od ľavého horného rohu virtuálnej pracovnej plochy. Pri úpravách výberu pixelov tieto súradnicové sústavy rozlišujte.

Prekrytie vlastní snímku a orezaný obrázok. Hlavné okno kopíruje zachytenú oblasť na neskoršiu analýzu; každá úloha na pozadí dostane ďalšiu vlastnú kópiu. Číslo požiadavky zabraňuje tomu, aby starší výsledok prepísal novší výber.

## Zostavenie a testovanie

```powershell
dotnet restore ChromaTray.Tests/ChromaTray.Tests.csproj --locked-mode
dotnet test ChromaTray.Tests/ChromaTray.Tests.csproj -c Release --no-restore
```

Úplná sada testov vyžaduje Windows. Na Linuxe/macOS spustite `ChromaTray.Core.Tests/ChromaTray.Core.Tests.csproj`, ktorý testuje spoločné algoritmy farieb, histogramu, navigácie a exportu. Linux dokáže aj skompilovať projekt pre Windows vďaka nastaveniu `EnableWindowsTargeting`.

## Zostavenie dokumentácie

Nainštalujte .NET 8 SDK a Python 3. Z koreňového priečinka repozitára spustite:

```powershell
dotnet tool restore
dotnet restore ChromaTray.csproj --locked-mode
python scripts/build-docs.py
```

Na Linuxe/macOS podľa potreby použite `python3` namiesto `python`. Hotový web sa uloží do `artifacts/docs-site`. Lokálny náhľad spustíte takto:

```powershell
dotnet docfx serve artifacts/docs-site --port 8080
```

Otvorte `http://localhost:8080/sk/`. Anglická verzia je v koreni webu; preklady sú v `/sk/`, `/cs/`, `/ru/` a `/uk/`. Každý jazyk má vlastný vyhľadávací index. Príručky upravujte v `docs/site/locales/<jazyk>` a texty rozhrania v súbore `template/token.json` v tom istom priečinku. Spoločná šablóna pridáva prepínač, ktorý zachová aktuálnu stránku.

API metadáta a HTML sa generujú automaticky a sú vylúčené z Gitu. Verzia DocFX je pevne určená v `.config/dotnet-tools.json`. Anglické popisy API aktualizujte úpravou komentárov `/// <summary>`, `<param>` a `<returns>` v C#. Bežné implementačné komentáre `//` zostávajú v zdrojovom kóde. Referencia zobrazuje verejné typy a členy; táto stránka vysvetľuje aj interné komponenty.

## Automatické aktualizácie

Workflow **Documentation** zostaví všetky jazyky pri zmenách a pull requestoch a uloží web do artefaktu `documentation-site`. Kontrola pull requestu web nepublikuje.

Na automatické publikovanie cez GitHub Pages musí správca repozitára jednorazovo:

1. V **Settings → Pages** nastaviť **Source: GitHub Actions**.
2. Otvoriť **Settings → Secrets and variables → Actions → karta Variables → New repository variable**. Zadať názov `DOCS_PUBLISH_ENABLED` a hodnotu `true`.
3. Spustiť workflow **Documentation** alebo odoslať zmenu dokumentácie či kódu do `main`.

Po nastavení úspešné zostavenia vetvy `main` publikujú web. Verejná adresa sa zobrazí v prostredí nasadenia workflow. Bez tohto nastavenia zostáva celý web dostupný na stiahnutie ako artefakt.
