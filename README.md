# Hitboxes — локальный лаунчер Minecraft: Java Edition

Набор из двух проектов:

- **`launcher/`** — нативный Windows-лаунчер (.NET 8 / WPF), по интерфейсу
  вдохновлённый Prism Launcher (список сборок/инстансов, создание новой
  сборки, настройки по умолчанию), но со своим форматом хранения. Офлайн-
  аккаунты, официальные версии Minecraft: Java Edition 1.16.5+, Fabric,
  автоустановка модов с Modrinth, тема день/ночь/дождь, музыка C418 в
  главном меню, сборка через GitHub Actions.
- **`mod/`** — Fabric-мод для Minecraft 1.16.5+ с отладочной панелью
  (клавиша **Right Shift**) для одиночной игры: авто-фарм мобов,
  визуализация и увеличение хитбоксов мобов, авто-поедание золотых/
  зачарованных яблок, пошаговый авто-строитель ферм.

## Сознательные ограничения (по договорённости)

Это **не** PvP/чит-клиент. Явно исключено и не будет добавляться:

- Автоатака или любое воздействие на других игроков (killaura по игрокам).
- Увеличение хитбоксов **игроков** — enlargement применяется только к
  немобам-животным/мобам (`AnimalEntity`/`HostileEntity`), никогда к
  `PlayerEntity` — см. `EntityHitboxMixin`.
- Скрытые/замаскированные функции — панель и все переключатели видимы в UI.
- Раздача/установка модифицированных ("с софтом") версий игры — лаунчер
  работает только с официальным `version_manifest_v2.json` от Mojang;
  моды ставятся только из открытого API Modrinth по явному запросу игрока.

Все автоматизации (авто-фарм, увеличение хитбоксов мобов, авто-строитель)
активны **только пока не обнаружено других игроков** рядом (см.
`SafetyGuard` в моде) — это одиночные/LAN-utility-фичи, не PvP-инструменты.

Музыка в главном меню лаунчера не поставляется вместе с лаунчером как
файл: она проигрывается только из `.ogg`, которые уже были официально
скачаны с CDN Mojang при установке версии игры (см.
`MainMenuMusicService`) — то есть ровно тот саундтрек C418, на который у
пользователя уже есть лицензия через саму игру.

## Сборка

- **Launcher**: `dotnet build launcher/Launcher.sln` (Windows, .NET 8 SDK).
- **Mod**: `./gradlew build` в каталоге `mod/` (нужен JDK 8/16+ и доступ к
  Fabric Maven для зависимостей — в этой песочнице сборка не запускалась,
  код написан и структурирован под Fabric Loom 0.10 / MC 1.16.5).

Оба проекта также собираются автоматически в CI — см.
`.github/workflows/build.yml` (запускается на push/PR и вручную,
артефакты — собранный `.exe`/`.jar` — прикладываются к запуску).

## Структура

```
launcher/                              WPF-приложение
  Launcher/Models/                      Instance, LauncherSettings, DTO манифестов
  Launcher/Services/
    InstanceService, SettingsService     хранение сборок и настроек лаунчера
    MinecraftVersionService, GameInstaller, GameLauncher   установка/запуск ванильных версий
    FabricInstallerService               установка Fabric Loader поверх ванильной версии
    ModrinthService                      поиск и установка модов
    MainMenuMusicService, ThemeService, WeatherService   музыка и тема день/ночь/дождь
  Launcher/Themes/                      ResourceDictionary для Day/Night/Rain
  Launcher/MainWindow, NewInstanceWindow,
    InstanceSettingsWindow, SettingsWindow   Prism-подобный UI

mod/                                    Fabric-мод
  src/main/java/.../feature/            авто-фарм мобов, визуализация хитбоксов, авто-еда
  src/main/java/.../feature/farmbuilder/  чертежи и авто-строитель ферм
  src/main/java/.../mixin/EntityHitboxMixin.java  увеличение хитбоксов мобов (не игроков)
  src/main/java/.../gui/                панель по Right Shift
  src/main/java/.../util/SafetyGuard.java  проверка "рядом нет других игроков"

.github/workflows/build.yml            сборка лаунчера и мода в CI
```
