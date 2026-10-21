# Запуск ChromaTray на своём компьютере

Эта инструкция рассчитана на Windows 10/11 x64. Интерфейс программы — на английском. Захват экрана и системный трей требуют обычного рабочего стола Windows: Linux, macOS и WSL не подходят для запуска интерфейса.

## 1. Самый простой способ: готовый файл

Возьмите автономный `ChromaTray-win-x64-standalone.exe` из результатов сборки или раздела Releases после публикации релиза. Он содержит .NET и запускается без установки SDK.

1. Сохраните EXE, например, в `C:\Tools\ChromaTray`.
2. Запустите его двойным щелчком.
3. Должна открыться тёмная панель, а в области уведомлений появиться иконка с тремя цветными кругами.
4. Нажмите **Ctrl + Shift + X**, наведите лупу на цвет, поправьте положение стрелками **← ↑ ↓ →** и нажмите **Enter**. Можно также выбрать цвет левым щелчком. HEX попадёт в буфер обмена; вставьте его в Блокнот через **Ctrl + V**.
5. Для палитры нажмите **Ctrl + Shift + Z** и протяните прямоугольник.

`lightweight.exe` — другой вариант сборки. Для него нужен **.NET 8 Desktop Runtime x64**. Обычный `.NET Runtime` или `ASP.NET Core Runtime` не заменяют Desktop Runtime.

Когда вместе с релизом есть `SHA256SUMS.txt`, проверьте файл в PowerShell:

```powershell
Get-FileHash .\ChromaTray-win-x64-standalone.exe -Algorithm SHA256
```

Сравните `Hash` со строкой соответствующего файла в `SHA256SUMS.txt`: регистр букв значения не имеет.

## 2. Запуск из исходников через PowerShell

### Установите инструменты

- **.NET 8 SDK**: <https://dotnet.microsoft.com/download/dotnet/8.0>. Выберите SDK x64 **8.0.4xx**, минимум 8.0.400; это требование `global.json`.
- **Git** нужен только для клонирования: <https://git-scm.com/downloads/win>. Если вы получили файлы архивом, Git не требуется.

После установки откройте **новое** окно PowerShell:

```powershell
dotnet --list-sdks
```

В списке должна быть строка `8.0.4xx`. Наличие только SDK 9 или 10 не выполняет pin проекта: установите SDK 8 рядом с ними. Разрешение выбора свежего патча 8.0.4xx задано в `global.json`.

### Получите файлы

Если проект уже у вас в папке, переходите к следующему шагу. Иначе, после загрузки исходников в репозиторий:

```powershell
cd $env:USERPROFILE\Documents
git clone https://github.com/Gh0st3r0k/ChromaTray.git
cd ChromaTray
```

Либо скачайте **Code → Download ZIP** на GitHub и распакуйте архив. После этого откройте PowerShell в папке, содержащей `ChromaTray.csproj`, `Program.cs` и `global.json`.

### Восстановите зависимости и запустите

```powershell
dotnet restore ChromaTray.csproj --locked-mode
dotnet run --project ChromaTray.csproj -c Release --no-restore
```

Первый запуск загружает необходимые пакеты из NuGet. API-ключи и файл `.env` не нужны. После появления панели PowerShell остаётся занят процессом; завершить приложение можно через **Exit** в трее.

## 3. Запуск в Visual Studio

1. Установите **Visual Studio 2022 17.11 или новее** с нагрузкой **.NET desktop development** и SDK .NET 8.0.4xx.
2. Выберите **Open a project or solution** и откройте `ChromaTray.csproj`.
3. Дождитесь восстановления NuGet.
4. Нажмите **F5** или **Ctrl + F5**.

Отдельный дизайнер форм не требуется: интерфейс создаётся в C#.

## 4. Краткое управление

| Что нужно сделать | Как |
| --- | --- |
| Один цвет | **Pick Point** / **Ctrl + Shift + X**, затем **Enter** или щелчок |
| Поправить выбор | **← ↑ ↓ →** — на один физический пиксель; удержание повторяет шаги |
| Палитра области | **Scan Region** / **Ctrl + Shift + Z**, затем выделение мышью |
| Отмена | **Esc** / правая кнопка мыши |
| Увидеть точные исходные оттенки | **Palette → Group similar shades** (под заголовком) или **Settings → Group similar shades (region only)** — снимите галочку. Пипетка точки всегда использует точный RGB. |
| Скопировать конкретный формат | **Copy** рядом с его значением |
| Скопировать всю палитру | Формат экспорта внизу → **Copy palette** |
| Изменить сочетания | **Settings** → щелчок по полю → новая комбинация → **Save Changes** |
| Вернуть окно | Иконка в области уведомлений, возможно под стрелкой `^` |
| Полный выход | Правый щелчок по иконке → **Exit** |

Для основного RGB отключите HEX в Settings. Первым используется включённый формат в порядке HEX, RGB, HSL, HSV, CMYK. Не меньше одного формата должно оставаться включено.

Таблица показывает основной формат, процент и число пикселей. Остальные включённые форматы выбранного цвета доступны справа. Стрелки останавливаются у края экрана и не входят в пустые промежутки между мониторами.

Снимок заморожен на момент вызова захвата. Если содержимое экрана изменилось, отмените захват и откройте его снова.

## 5. Тесты

Полный набор запускается **на Windows**:

```powershell
dotnet restore ChromaTray.Tests/ChromaTray.Tests.csproj --locked-mode
dotnet test ChromaTray.Tests/ChromaTray.Tests.csproj -c Release --no-restore --logger "console;verbosity=normal"
```

Алгоритмы без интерфейса можно проверить на Windows, Linux или macOS:

```powershell
dotnet restore ChromaTray.Core.Tests/ChromaTray.Core.Tests.csproj --locked-mode
dotnet test ChromaTray.Core.Tests/ChromaTray.Core.Tests.csproj -c Release --no-restore
```

Для ручной проверки используйте [чек-лист Windows](WINDOWS-CHECKLIST.md). Тесты и успешная сборка не заменяют проверку захвата реального рабочего стола.

## 6. Собрать EXE

Автономная версия, без отдельной установки .NET на целевом компьютере:

```powershell
dotnet publish ChromaTray.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o artifacts/standalone
```

Результат: `artifacts\standalone\ChromaTray.exe`.

Облегчённая версия, с требованием Desktop Runtime x64 на целевом компьютере:

```powershell
dotnet publish ChromaTray.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o artifacts/lightweight
```

Результат: `artifacts\lightweight\ChromaTray.exe`.

## 7. Если что-то не работает

| Симптом | Что проверить |
| --- | --- |
| `dotnet` не найден | Установите SDK и откройте новое окно PowerShell. |
| `A compatible .NET SDK was not found` | `dotnet --list-sdks`: нужен SDK 8.0.4xx согласно `global.json`. |
| Просит установить .NET | Для lightweight нужен **Desktop Runtime 8 x64**; либо возьмите standalone. |
| Нет `ChromaTray.csproj` | Откройте терминал в корне распакованного проекта, а не в его родительской папке. |
| Хоткей не работает | Посмотрите строку состояния; сочетание может занимать другое приложение. Измените его в Settings. Кнопки захвата продолжают работать. |
| «Already running» | Найдите иконку под стрелкой `^`, откройте существующий экземпляр или выберите Exit. |
| Окно пропало после нажатия X | Так задумано: приложение осталось в трее. |
| Буфер обмена занят | Повторите Copy после завершения копирования другой программой. |
| Ошибка сохранения настроек | Проверьте доступ к `%LocalAppData%\ChromaTray`. |
| NuGet не восстанавливается | Проверьте доступ к `api.nuget.org` и `globalcdn.nuget.org`, время системы и доверенные сертификаты. Не отключайте проверку TLS. |
| Защищённое видео/экран блокировки не захватывается | Это ограничение захвата Windows; проверяйте на обычном рабочем столе. |

Сброс настроек: сначала **Exit** в трее, затем переименуйте `%LocalAppData%\ChromaTray\settings.json`, например в `settings.backup.json`, и снова откройте приложение.

## 8. Облачная Linux-среда

В подготовленной среде SDK находится в `/workspace/.dotnet`. Команды для восстановления среды и проверки:

```sh
bash scripts/setup-cloud.sh
bash scripts/check-cloud.sh
```

Скрипты устанавливают проверенный по SHA-512 SDK, собирают Windows-проект и запускают тесты алгоритмов. Они не запускают Windows-интерфейс. При запрете системных mutex песочницей агенту может потребоваться разрешение на выполнение команд .NET вне песочницы.
