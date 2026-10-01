# Poprawki po review — 2026-10-01

Branch: `stage-5-optional`. Punkt wyjścia: `f515eff`.
Numery odpowiadają [raportowi review](review-2026-09-29.md).
Uwagi 1 i 25 były naprawione w poprzednim commicie. Uwagi 2–24 wdrożono w tej zmianie.

## Realizacja

| Uwagi | Poprawka i weryfikacja |
| --- | --- |
| 1 | Obowiązkowy verdict i ścisły parser odpowiedzi modelu; wcześniejsze testy regresji pozostają zielone. |
| 2 | OrderPlaced przenosi snapshot z identyfikatorami i wersjami pozycji. Konsument usuwa tylko niezmienione zamówione pozycje. Wszystkie mutacje koszyka współdzielą blokadę właściciela; wersja pozycji rośnie także przy nieruchomym/cofniętym zegarze. Testy opóźnionej dostawy, edycji pozycji i replay z innym MessageId. |
| 3–4 | Blokady opinii przed odczytem we wszystkich ścieżkach moderacji, przepisania i zgłoszenia; cleanup także blokuje usuwane opinie. Agregaty serializowane per produkt i wersjonowane trwałym zegarem o mikrosekundowej precyzji. Test równoległej moderacji porównuje Review API i rzeczywisty Catalog API, także po dostarczeniu agregatów w odwrotnej kolejności. |
| 5 | Blokada limitu per autor/sesja i oddzielne ReviewSubmissions liczące każdą próbę, także przepisanie odrzuconej opinii. Testy równoległych zgłoszeń i ponownych prób. |
| 6 | Transakcje obejmują dane i audyt w ContentStore, opiniach, koszyku oraz analogicznych CRUD-ach produktów, kodów rabatowych i adresu konta. Pomocnik współdzieli istniejącą transakcję konsumenta. Testy awarii audytu potwierdzają rollback create/update/delete treści. |
| 7 | Tylko naruszenie konkretnego indeksu sluga jest mapowane na 409. Test równoległego tworzenia daje 201 i 409. |
| 8 | Blokada właściciela obejmuje odczyt pojemności i zapis wishlisty. Test dwóch równoległych dodatków do prawie pełnej listy. |
| 9 | Wspólna walidacja zakresu, retencja od północy UTC, dashboard korzysta z rzeczywistego zakresu lejka przy pobieraniu zamówień i opisach. Test zachowania pełnego dnia na granicy retencji. |
| 10 | Trwałe OrderReceipts deduplikują OrderPlaced po OrderId niezależnie od okna inboxa i retencji audytu. Lejek liczy unikalne identyfikatory zamówień. Test replay z innym transportowym MessageId. |
| 11 | Ponowne odczytanie po skutecznym start/retry SignalR oraz uzgadnianie co 15 s, gdy dashboard jest otwarty. Testy połączenia, retry i opóźnionej projekcji. |
| 12 | Szerokość słupka ograniczona do 100%; liczby i procenty zachowują rzeczywistą wartość. Dashboard wyjaśnia charakter liczników zdarzeń. Test wartości ponad 100%. |
| 13 | Metryka to wartość złożonych zamówień, obejmująca także nieopłacone, anulowane i zwrócone. UI opisuje tę definicję; top products pokazuje wartość pozycji przed rabatami. Nazwy pól Revenue zachowane dla zgodności API. Test statusów i rabatu. |
| 14 | Zakres 1–3650 dni; nieprawidłowe wartości dają 422 zamiast wyjątku daty. Testy 0, -1 i int.MaxValue. |
| 15 | Timeout obejmuje body i refresh. Współdzielona obietnica refreshu jest zawsze zwalniana; awarie sieci/5xx zachowują sesję, odmowa 401/403 ją kończy. Testy zawieszonego body, refreshu i powrotu po przejściowej awarii. |
| 16 | Audyt sortowany po Timestamp, potem Id. Test stronicowania przy jednakowych timestampach. |
| 17 | OrderCheckout i OrderStatistics wydzielone z OrderService i wstrzykiwane przez DI; odpowiedzialność za administracyjne zmiany statusu pozostaje w OrderService. |
| 18 | Wspólny shell formularza przyjmuje typowany komponent pól i Partial konkretnego payloadu. Usunięto rzutowania unii w mutacjach formularza i mapowaniu treści. TypeScript i scenariusze CRUD sprawdzają wynik. |
| 19 | Test kontraktowy C#/TS obejmuje ścieżkę huba, nazwę zdarzenia, pola payloadu i wartości statusów. |
| 20 | Walidowane opcje RabbitMQ i timeoutów UI; kolory wykresu używają tokenów motywu; limit długości ShopEvent ma nazwaną stałą. |
| 21 | Polityka transakcji/providerów przeniesiona do StoreTransactions. Relacyjne blokady wymagają aktywnej transakcji; testy współbieżności nadal działają na PostgreSQL. |
| 22 | CancellationToken przekazywany z kontrolerów do operacji zamówień, treści i wishlisty, dalej do EF/HTTP/publikacji. Snapshoty różnych produktów checkoutu pobierane równolegle przed transakcją. |
| 23 | DeliveryOptions waliduje Min <= Max. E2E czyta daty historii z rzeczywistego zamówienia, a kupony tworzy jako kontrolowane dane z względnymi datami. Własna definicja CET/CEST i pomijanie świąt pozostają świadomą regułą dostawy. |
| 24 | Każdy host ma osobne endpointy; bus kończy dostawy przed anulowaniem aplikacji i disposal usług, zarówno przy Dispose, jak i DisposeAsync. Testy używają loggera konsolowego zamiast współdzielonego przez kontekst busa zwolnionego Windows EventLog. Probe’y będące wyłącznie obserwatorami mają osobne endpointy bez trwałego EF inboxa. Produkcyjni konsumenci nadal używają rzeczywistego inboxa/outboxa. Nie wydłużono timeoutów ani nie dodano retry do testów. |
| 25 | Wcześniejsza normalizacja CRLF w kontrolach OpenAPI/TS pozostaje zachowana. Kontrakty wygenerowano ponownie po zmianie snapshotu koszyka. |

## Migracje i zgodność

- ReviewConcurrency dodaje ReviewSubmissions i ReviewSummaryClocks. Obecne opinie autorów inicjalizują liczniki prób; wcześniejszych nadpisanych prób nie można odtworzyć. Cleanup usuwa historię prób starszą niż okno limitu.
- OrderReceipts dodaje trwały klucz deduplikacji i inicjalizuje go z istniejących wpisów audytu. Wpisy usunięte przez wcześniejszą retencję są nieodtwarzalne. Klucze pozostają po późniejszym usunięciu audytu.
- Starsze OrderPlaced bez snapshotu są obsługiwane bez czyszczenia koszyka. Przy wdrożeniu odbiorcę koszyka należy zaktualizować przed producentem zamówień; stare oczekujące komunikaty mogą pozostawić zakupione pozycje do ręcznego usunięcia. Zmieniona pozycja także pozostaje w całości — priorytetem jest zachowanie nowej zawartości.
- Zakresy zamówień i lejka są wspólne, ale projekcja audytu pozostaje asynchroniczna. Dashboard uzgadnia dane okresowo; chwilowa różnica jest możliwa.
- CancellationToken działa do zakończenia I/O i zatwierdzenia transakcji. Po zatwierdzeniu anulowany HTTP nie cofa operacji; checkout można powtórzyć z tym samym kluczem idempotencji.

## Strojenie konfiguracji

RabbitMQ obsługuje OutboxQueryDelaySeconds, DuplicateDetectionMinutes, PrefetchCount oraz RetryCount/RetryMinSeconds/RetryMaxSeconds/RetryDeltaSeconds w sekcji RabbitMQ. Wartości są walidowane przy starcie.
UI otrzymuje REQUEST_TIMEOUT_MS (1000–120000), RENEW_BEFORE_EXPIRY_MS (0–300000) i DASHBOARD_DAYS (1–3650) przez config.js generowany przy starcie kontenera. Compose przekazuje zmienne środowiskowe; domyślne wartości to odpowiednio 15000, 30000 i 30. Zmiana nie wymaga przebudowania SPA, wymaga odtworzenia kontenera z nowym środowiskiem.

## Weryfikacja

| Kontrola | Wynik | Dowód lokalny (ignorowany przez Git) |
| --- | --- | --- |
| Backend Release, analizatory i formatowanie | sukces, bez ostrzeżeń kompilatora | `TestResults/fixes-build-final.log`, `fixes-format.log`; końcowy `dotnet test` ponownie kompilował projekty |
| Jednostkowe backendu | 610/610 | `TestResults/fixes-unit-final.log`, `Tests/Unit/TestResults/fixes-unit-final.trx` |
| Pełne integracyjne, PostgreSQL/Testcontainers | 283/283, proces zakończony kodem 0 | `TestResults/fixes-integration-final.log`, `Tests/Integration/TestResults/fixes-integration-final.trx` |
| Jednostkowe/komponentowe UI | 178/178, 39 plików; maxWorkers=2 | `TestResults/fixes-ui-tests-final.log` |
| TypeScript i ESLint | sukces | `TestResults/fixes-types-final.log`, `fixes-lint-final.log` |
| UI build i budżet JS | sukces; 418 kB / 500 kB, 659 kB z preloadami | `TestResults/fixes-ui-build-final.log`, `fixes-bundle-final.log` |
| Eksport OpenAPI i api:check | aktualne | `TestResults/fixes-openapi-check-final.log`, `fixes-api-check.log` |
| Modele EF vs migracje Review/Audit | brak niezapisanych zmian modelu | `TestResults/fixes-model-review-final.log`, `fixes-model-audit-final.log` |
| Compose, migracje i runtime config UI | sukces; prawidłowe ustawienia przyjęte, timeout 0 odrzucony | `TestResults/fixes-compose-final.log`, `fixes-compose-ui.log`, `fixes-compose-dev-final.log`, `fixes-runtime-valid.log`, `fixes-runtime-invalid.log` |
| Pełne E2E na przebudowanym compose UI | 28/28, 1.9 min; bez retries | `TestResults/fixes-e2e-final.log`, `UI/store-app.UI/playwright-report/index.html` |
| git diff --check | sukces | kontrola przed commitem |

Końcowy pełny log integracyjny nie zawiera ObjectDisposedException, R-FAULT, T-FAULT ani błędów cleanup fixture. Błędy łączności w teście celowego wyłączenia PostgreSQL oraz brak tabeli historii przed pierwszą migracją są oczekiwanymi elementami tych scenariuszy.
E2E obejmuje rzeczywisty checkout, płatności, live orders, treści, moderację, SEO oraz porównanie lejka i dashboardu przed/po checkout. Test lejka czyta stabilne odpowiedzi API i osobno sprawdza wyrenderowane liczniki, ponieważ połączenie live może zastąpić początkowe żądanie przeglądarki.

E2E korzystało z profilu deweloperskiego (konta demo) i tymczasowo zwiększonych limitów gateway dla automatycznych żądań. Testy integracyjne limitowania nadal sprawdzają 429 i Retry-After. Po E2E odtworzono gateway ze zwykłą konfiguracją deweloperską. Hasło administratora pobrano z lokalnej konfiguracji uruchomionego kontenera bez zapisywania go w repozytorium lub logach.
Storybook/visual nie powtarzano: zmienione formularze, dashboard i lejek nie mają stories. Poprzedni pełny przebieg wizualny jest opisany w raporcie review.

## Zależności poza tą zmianą

Zdjęcia, prawdziwe API moderacji bez klucza, ponowna walidacja Rich Results na publicznych URL-ach i konfiguracja Azure pozostają zależnościami wcześniejszego planu. Nie są zamknięte przez testy lokalne. Nie wykonano merge, push ani CD.
Zastany nieśledzony `docs/image-prompts.md` pozostaje plikiem użytkownika i nie wchodzi do commita.
