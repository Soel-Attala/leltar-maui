# Leltár – Inventory (.NET MAUI)

[English](#english) · [Magyar](#magyar)

---

## English

A small .NET MAUI app that manages a locally stored product list with SQLite: product list, search,
adding a new product and a detail page for editing.

### Running

Requirements: .NET 10 SDK with the MAUI workload (`dotnet workload install maui`).

```bash
# Windows
dotnet build src/Leltar -f net10.0-windows10.0.19041.0 -t:Run

# Android (emulator or device)
dotnet build src/Leltar -f net10.0-android -t:Run

# Unit tests
dotnet test
```

To try the list with thousands of items, use **⋯ → Load demo data** on the main page (adds 5,000 products,
about 10% of them out of stock).

### Project structure

```
src/Leltar.Core     net10.0 class library: model, SQLite repository, validation, view models
src/Leltar          MAUI app: pages (XAML), Shell navigation, platform services
tests/Leltar.Tests  xUnit tests for the repository, validation and view models
```

### Technical decisions

- **MVVM with CommunityToolkit.Mvvm.** Source-generated observable properties and commands keep view models short.
- **View models live in a separate, plain `net10.0` library.** They depend only on small interfaces
  (`IProductRepository`, `INavigationService`, `IDialogService`), so they can be unit tested without a device or
  emulator. The MAUI project only contains views and the Shell-based implementations of these interfaces.
- **sqlite-net-pcl** for data access: lightweight, async, the de facto standard for MAUI. The connection is
  initialized lazily (WAL mode, table creation) on first use and the repository is a DI singleton.
- **Large lists:** a virtualized `CollectionView` with *paging* (50 items per page, the next page loads via
  `RemainingItemsThreshold`). Only what is needed is in memory, and the first screen appears instantly even with
  tens of thousands of rows. `MeasureFirstItem` sizing avoids measuring every row.
- **Search runs in the database**, not in memory, with a 300 ms debounce while typing. Results from an older query
  are discarded (query versioning), so fast typing cannot show stale results.
- **Accent- and case-insensitive search:** the repository stores a normalized copy of the name (`SearchName`:
  lowercase, no diacritics), so `arvizturo` finds `Árvíztűrő`. The same indexed column is used for sorting.
  LIKE wildcards (`%`, `_`) in the search term are escaped.
- **Out-of-stock products** (quantity ≤ 0) get a light red row background and a red "Out of stock" label, via
  `DataTrigger`. It does not rely on color alone and works in both light and dark theme.
- **Unique barcode:** enforced by a unique index in the database; the repository turns the constraint error into a
  `DuplicateBarcodeException`, which the form shows as a field error.
- **Keeping the list in sync:** after saving, the form sends a message (`IMessenger`). The list replaces an edited
  item in place (scroll position is kept), removes a deleted item, and reloads after an insert (the position of a new
  item in the sorted list is not known).
- **Shared form:** adding and editing use the same `ProductFormView` and a common base view model
  (`ProductFormViewModel`); validation is in a separate, tested `ProductValidator`.
- **Number input** accepts both a decimal comma and a decimal point, and spaces as thousands separators
  (`1 299,5`), since Hungarian users usually type a comma.
- The UI is in English; Hungarian translations are given in short code comments.

### What I would do differently with more time

- **Decimal storage:** sqlite-net stores `decimal` as `REAL` (double). This is fine for prices and quantities of
  this size, but for exact money arithmetic I would store integer minor units (e.g. fillér) or text.
- **Keyset pagination** instead of `OFFSET`: faster on very large tables and stable when rows are added or
  renamed while scrolling.
- **FTS5 full-text index** for search: `LIKE '%term%'` cannot use an index, which is fine for thousands of rows but
  not for hundreds of thousands.
- **Schema migrations** with an explicit version table instead of relying on `CreateTable` only.
- **Localization** with `.resx` resources (English/Hungarian) instead of hard-coded strings.
- Warn about **unsaved changes** when leaving the edit page.

### Further improvements

- Barcode scanning with the camera (e.g. ZXing.Net.Maui) for search and data entry.
- Quick stock adjustment (+/−) and a stock movement history.
- Filters (out of stock only, low stock) and sorting options.
- Product categories, units of measure and images.
- CSV/Excel import and export, cloud sync / backend API.
- UI tests (Appium) and a CI pipeline (GitHub Actions) for build and tests.

---

## Magyar

Kisebb .NET MAUI alkalmazás, amely lokális SQLite adatbázisban tárolt terméklistát kezel: terméklista, keresés,
új termék felvétele, valamint részletező oldal a termék módosításához.

### Futtatás

Követelmény: .NET 10 SDK MAUI workloaddal (`dotnet workload install maui`).

```bash
# Windows
dotnet build src/Leltar -f net10.0-windows10.0.19041.0 -t:Run

# Android (emulátor vagy eszköz)
dotnet build src/Leltar -f net10.0-android -t:Run

# Unit tesztek
dotnet test
```

A lista több ezer elemmel való kipróbálásához a főoldalon a **⋯ → Load demo data** menüpont 5000 terméket ad
hozzá (kb. 10%-uk elfogyott).

### Projektstruktúra

```
src/Leltar.Core     net10.0 osztálykönyvtár: modell, SQLite adattár, validálás, ViewModelek
src/Leltar          MAUI alkalmazás: oldalak (XAML), Shell navigáció, platformszolgáltatások
tests/Leltar.Tests  xUnit tesztek az adattárhoz, a validáláshoz és a ViewModelekhez
```

### Technikai döntések

- **MVVM a CommunityToolkit.Mvvm segítségével.** A forrásgenerált tulajdonságok és parancsok rövidebbé teszik a
  ViewModeleket.
- **A ViewModelek külön, sima `net10.0` könyvtárban vannak.** Csak kis interfészektől függenek
  (`IProductRepository`, `INavigationService`, `IDialogService`), így eszköz vagy emulátor nélkül unit tesztelhetők.
  A MAUI projekt csak a nézeteket és ezen interfészek Shell alapú megvalósítását tartalmazza.
- **sqlite-net-pcl** az adateléréshez: könnyű, aszinkron, MAUI-ban bevett megoldás. A kapcsolat első használatkor
  inicializálódik (WAL mód, táblalétrehozás), az adattár singletonként van regisztrálva.
- **Nagy listák:** virtualizált `CollectionView` *lapozással* (oldalanként 50 elem, a következő oldal a
  `RemainingItemsThreshold` segítségével töltődik be). Csak a szükséges adat van a memóriában, és az első képernyő
  több tízezer sor esetén is azonnal megjelenik. A `MeasureFirstItem` beállítás miatt nem kell minden sort lemérni.
- **A keresés az adatbázisban fut**, nem a memóriában, gépelés közben 300 ms késleltetéssel (debounce). Egy régebbi
  lekérdezés eredményét az alkalmazás eldobja (lekérdezés-verziózás), így gyors gépelésnél sem jelenik meg elavult
  találat.
- **Ékezet- és kisbetű/nagybetű-független keresés:** az adattár a név normalizált másolatát is tárolja
  (`SearchName`: kisbetűs, ékezet nélküli), így az `arvizturo` megtalálja az `Árvíztűrő` terméket. Ugyanez az
  indexelt oszlop szolgál a rendezéshez. A keresőkifejezésben a LIKE helyettesítő karakterei (`%`, `_`)
  escape-elve vannak.
- **Elfogyott termékek** (mennyiség ≤ 0): halvány piros sorháttér és piros „Out of stock” (Elfogyott) felirat,
  `DataTrigger` segítségével. Nem csak a színre épít, és világos és sötét témában is működik.
- **Egyedi vonalkód:** az adatbázisban egyedi index biztosítja; az adattár a megszorítás hibáját
  `DuplicateBarcodeException` kivétellé alakítja, amelyet az űrlap mezőhibaként jelenít meg.
- **A lista szinkronban tartása:** mentés után az űrlap üzenetet küld (`IMessenger`). A lista a módosított elemet
  helyben cseréli (a görgetési pozíció megmarad), a törölt elemet eltávolítja, új elem után pedig újratölt (az új
  elem helye a rendezett listában nem ismert).
- **Közös űrlap:** a felvétel és a módosítás ugyanazt a `ProductFormView` nézetet és közös ősosztályt
  (`ProductFormViewModel`) használja; a validálás külön, tesztelt `ProductValidator` osztályban van.
- **Számbevitel:** tizedesvesszőt és tizedespontot is elfogad, a szóközt ezres tagolásként kezeli (`1 299,5`), mivel
  a magyar felhasználók általában vesszőt írnak.
- A felhasználói felület angol nyelvű; a magyar fordítások rövid kódkommentekben szerepelnek.

### Mit csinálnék másképp több idővel

- **Decimal tárolás:** a sqlite-net a `decimal` típust `REAL` (double) formában tárolja. Ekkora árak és mennyiségek
  esetén ez megfelelő, de pontos pénzügyi számításokhoz egész értékű legkisebb egységben (pl. fillér) vagy szövegként
  tárolnám.
- **Keyset lapozás** `OFFSET` helyett: nagyon nagy táblán gyorsabb, és görgetés közbeni beszúrásnál vagy
  átnevezésnél is stabil.
- **FTS5 teljes szöveges index** a kereséshez: a `LIKE '%kifejezés%'` nem tud indexet használni; ez néhány ezer
  sornál nem gond, több százezernél már igen.
- **Sémamigrációk** explicit verziótáblával, nem csak a `CreateTable` hívásra hagyatkozva.
- **Lokalizáció** `.resx` erőforrásokkal (angol/magyar) a beégetett szövegek helyett.
- Figyelmeztetés **mentetlen módosításokra** a szerkesztő oldal elhagyásakor.

### További fejlesztési lehetőségek

- Vonalkód beolvasása kamerával (pl. ZXing.Net.Maui) kereséshez és adatbevitelhez.
- Gyors készletmódosítás (+/−) és készletmozgás-napló.
- Szűrők (csak elfogyott, alacsony készlet) és rendezési lehetőségek.
- Termékkategóriák, mértékegységek és képek.
- CSV/Excel import és export, felhőszinkron / backend API.
- UI tesztek (Appium) és CI pipeline (GitHub Actions) a buildhez és a tesztekhez.
