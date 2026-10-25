# Playbook: „kinowa” sesja z Higgsfield w aplikacji webowej

Dokument, który chciałbym dostać przed startem sesji z 24–25.10.2026 w `store-app`: dwie rundy
(15 commitów, 57 generacji, ~775 kredytów w sumie). Skopiuj go do nowego repo (np.
`docs/cinematic/PLAYBOOK.md`), dopisz krótki `BRIEF.md` dla tej aplikacji i daj oba Claude'owi na
start. Liczby i ceny są z października 2026, więc ceny przed użyciem sprawdź poleceniem
`higgsfield generate cost`.

---

## 1. Najważniejsze w 12 punktach

1. **Kończ ujęcie na prawdziwym zdjęciu aplikacji, generuj tylko początek.** Klatka końcowa = zdjęcie
   produktu lub wnętrza z repo, a startowa to wygenerowany „przedsionek” (drzwi, korytarz, świt,
   pusty pokój). Wtedy w filmie nie pojawia się nic, czego nie ma w aplikacji, a przejście z wideo na
   prawdziwe zdjęcie (z hotspotami, linkami) jest bez skoku.
2. **Kling O3 FLF (`kling_o3_flf`) to główny koń roboczy:** 6,25 kr za 5 s, 12,5 za 10 s, tryb pro.
   Dla pętli „rusza się tylko światło” (start = koniec) i dla wejść przez drzwi był tak wierny jak
   Seedance, przy kilkunastokrotnie niższym koszcie. Obsługuje tylko proporcje 16:9, 9:16 i 1:1.
3. **Seedance 2.5 tylko do trudnego ruchu kamery.** Najpierw szkic `--draft true` (15–18 kr), finał
   przez `--draft_job_id` (6 s = 96 kr). **Finał ze szkicu renderuje się zawsze w 1080p**, niezależnie
   od `--resolution`, więc koszt pytaj z tym samym `--draft_job_id`.
4. **GPT Image 2.5 do klatek startowych:** edycja istniejącego zdjęcia („cofnij kamerę za
   drzwi”, „to samo zdjęcie o świcie”, „poszerz do kwadratu”). Meble zostają, bo model dostaje
   referencję. Koszt: high 2k 2,75, high 1k 1,5.
5. **Każdy klip przed użyciem oglądaj jako arkusz klatek (ffmpeg `tile`).** Morfingi, przenikania i
   wymyślone meble widać od razu. Szkic nie ogląda się sam.
6. **Generuj po jednym zadaniu na wiadomość/komendę.** Nawet dwa wywołania w jednej wiadomości
   mogą pójść równolegle. Długie generacje puszczaj w tle (`run_in_background`) i w tym czasie
   pisz kod.
7. **Hook budżetowy z progiem** (`Scripts/claude/higgsfield-budget-guard.mjs`, punkt 3) blokuje
   każde `generate create`, które zeszłoby poniżej progu. Próg ustawiasz przez `env` w
   `.claude/settings.local.json`.
8. **`account status` nie pokazuje wszystkiego.** Po upgradzie planu Higgsfield dał osobną pulę 200
   kredytów „Concurrent Boost”, zużywaną przed saldem głównym. Prawdę pokazuje
   `higgsfield account transactions`.
9. **LCP zostaje obrazem.** Wideo na stronach mierzonych przez Lighthouse ładuje się dopiero po
   pierwszej interakcji (scroll, ruch myszy, dotyk, klawisz) albo przy zbliżeniu do ekranu.
   Lighthouse nie scrolluje, więc wideo się nie pobiera.
10. **Każdy efekt za flagą w jednym pliku** (`src/lib/motion.ts`). Przy `prefers-reduced-motion`,
    Data Saver i (zwykle) na telefonie: sam plakat.
11. **ffmpeg + WebP: `-quality` jest ignorowane.** Używaj `-q:v 80 -compression_level 6`, inaczej
    plakaty mają 3–10 kB i są rozmyte.
12. **Weryfikacja w prawdziwym Chrome.** Chromium z Playwrighta nie odtwarza H.264, a ukryty panel
    przeglądarki w aplikacji nie odtwarza wideo. Zrzuty rób skryptem Playwright z
    `channel: 'chrome'`.

---

## 2. Przygotowanie (zanim cokolwiek wygenerujesz)

| Rzecz | Jak | Uwagi |
| --- | --- | --- |
| Higgsfield CLI | `higgsfield account status`, `higgsfield account transactions --size 30` | logowanie: `higgsfield auth login` (interaktywnie, robi właściciel) |
| Hook budżetu | skopiuj `Scripts/claude/higgsfield-budget-guard.mjs` i wpis z `.claude/settings.local.json` (niżej) | blokuje też `higgsfield website …`; loguje do `.claude/higgsfield-budget.log` |
| ffmpeg | `ffmpeg -version`; brak → `winget install Gyan.FFmpeg` | kodowanie, arkusze klatek, plakaty |
| Playwright | `npx playwright install chromium` (gdy brak przeglądarek) | do wideo i tak `channel: 'chrome'` |
| Docker | `docker compose … up -d --no-build` | stos aplikacji, Lighthouse i testy wizualne w kontenerze |
| Emulator Androida | AVD z Android SDK (`emulator -list-avds`) | punkt 8 |
| `.gitignore` | `/media-work` w katalogu UI | surowe media i zrzuty poza gitem |

```json
// .claude/settings.local.json
{
  "env": { "HF_CREDIT_FLOOR": "1500" },
  "hooks": {
    "PreToolUse": [
      { "matcher": "Bash|PowerShell",
        "hooks": [{ "type": "command", "command": "node <repo>/Scripts/claude/higgsfield-budget-guard.mjs" }] }
    ]
  }
}
```

**Hook:** przed zadaniem pyta `account status --json` o saldo i `generate cost` (to samo polecenie z
`cost` zamiast `create`) o koszt. Gdy nie odczyta kosztu (komendy z `cd … &&`, ścieżkami, pipe'ami),
przyjmuje bezpieczne 150 (`HF_MAX_JOB_COST`), co blisko progu blokuje za wcześnie. W 10 z 13 wywołań
rundy 1 korzystał z tego zastępczego kosztu. Nigdy nie obchodź hooka (np. wołając generację przez
skrypt, którego treści hook nie widzi).

**Skille, z których korzystałem:**
- **`higgsfield-generate`:** modele, flagi mediów, `--wait --json`.
- **`higgsfield-websites` (tylko jako wiedza):** `references/scroll-scrub*.md` (kontrakt materiału do
  scroll-scrubu, kodowanie GOP, seam-lock łańcucha klipów), `wow-catalog.md` (katalog technik).
  Jego scaffold i deploy są zablokowane.
- **`frontend-design`:** dyscyplina projektowa (jeden mocny akcent zamiast rozsypanych efektów, bez
  szablonowych tików).

**Research:** subagent w tle (`general-purpose`) z instrukcją „dopisz sekcję do `inspiration.md`,
nie dotykaj innych plików, nie uruchamiaj higgsfield”. Strony Awwwards nie opisują ruchu słowami
(trzeba wnioskować z tagów), a transkrypcji YouTube nie dało się pobrać.

**Gust właściciela zbierz na starcie, zanim dobierzesz efekty.** Tutaj podobały mu się Lenis i
wjeżdżające nagłówki (SplitText), a nie podobały się magnetyczne przyciski, pierścienie za kursorem,
paski z przesuwającym się tekstem (marquee) i tilt 3D.

---

## 3. Higgsfield: modele, ceny, co działało

### Cennik użytych narzędzi (październik 2026)

| Model / narzędzie | Do czego | Koszt | Wyjście |
| --- | --- | --- | --- |
| `kling_o3_flf` pro | pętle światła, wejścia przez drzwi, start/koniec | 6,25 (5 s), 12,5 (10 s); 4k: 30/60 | 16:9 → 1928×1076, 1:1 → 1440×1440 |
| `kling3_0` pro 10 s | alternatywa | 17,5–25 | – |
| `seedance_2_5` draft | szkic ruchu | 15 (5 s), 18 (6 s), 24 (8 s), 30 (10 s) | ~560–750 px |
| `seedance_2_5` finał (`--draft_job_id`) | finał | 80 (5 s), 96 (6 s), 128 (8 s), 160 (10 s) | 1080p, np. 4:3 → 1664×1248 |
| `gpt_image_2_5` | klatki startowe, edycje, makro, puste wnętrza | low 1k 0,25 · high 1k 1,5 · high 2k 2,75 | 2k 16:9 ≈ 2752×1536 |
| `tripo_h3_1_image_to_3d` | model 3D z pakshotu | 9 (standard), 18 (detailed) | GLB 68 MB (!) |
| `nano_banana_2_relight` | to samo zdjęcie w innym świetle | 2 | (nieużyty, GPT Image wystarczył) |
| `image_decompose` (workflow) | rozbicie zdjęcia na warstwy do paralaksy | 2 | (nieużyty) |
| `image_background_remover` | wycięcie obiektu | 1 | (nieużyty) |
| `seed_audio` | dźwięk | 0,1 | odrzucony: dźwięk w sklepie to gadżet |

Pełna lista: `higgsfield model list --json`, parametry: `higgsfield model get <job_type> --json`,
workflowy: `higgsfield workflow list`.

### Co zadziałało dobrze

- **Pętla „tylko światło”, Kling FLF, start = koniec = prawdziwe zdjęcie.** Kamera stoi, a słońce,
  cień liści, firanka lub pył w smudze się ruszają. Meble zostają identyczne, więc pętla jest
  bezszwowa i bezpieczna dla produktów.
- **Wejście do pokoju:**
  1. GPT Image robi z prawdziwego zdjęcia ujęcie „z korytarza przez szeroką dębową futrynę” (pokój w
     kadrze drzwi bez zmian).
  2. Kling FLF jedzie od tej klatki do prawdziwego zdjęcia.
  3. Ostatnia klatka Klinga praktycznie pokrywa się ze zdjęciem.
- **Zmiana pory dnia na tym samym zdjęciu** (GPT Image: „relight this exact photograph… blue hour /
  sunset; every object identical”), a potem Kling lub Seedance między wersjami. Tak powstały
  rejestracja (świt w pokoju) i ciemny motyw (salon o zachodzie).
- **Seedance draft → finał** dla ruchu „drzwi same się otwierają, kamera wchodzi, zatrzymuje się na
  komodzie z lampą” (logowanie). Finał wiernie powtórzył zaakceptowany szkic.
- **Puste przestrzenie** (404, puste stany): „completely empty room, no furniture, no objects”. To
  zero ryzyka dla produktów i świetnie pasuje do „pustych” ekranów.
- **Makro materiałów** (dąb, len, bouclé…): GPT Image „just the material filling the frame, no
  object”, potem Kling 5 s ze światłem przesuwającym się po fakturze.
- **Seam-lock łańcucha:** druga część startuje od **dokładnie ostatniej klatki** pierwszej
  (`ffmpeg -vf reverse -frames:v 1`), a potem `concat`. Spoina jest niewidoczna.

### Co nie zadziałało albo wymagało obejścia

- **Seedance z końcem na makro detalu** (z pokoju do zbliżenia lampy w 5 s) dał przenikanie zamiast
  ruchu i dorysował sofę ze stolikiem. Poprawka: koniec na zdjęciu *pokoju*, a „dojazd” w detal
  zrobiony w kodzie (CSS scale ostatniej klatki + przenikanie w zdjęcie detalu).
- **Kling między bardzo różnymi scenami** (las → warsztat → pokój) robi przenikanie przez poświatę,
  a nie ciągły ruch. W scroll-scrubie da się to obronić jako „rozdziały”, ale nie jako jedno ujęcie.
- **Kling przesadza z „breeze”:** firanka faluje mocniej, niż w prompcie. Pisz „barely stirs”.
- **GPT Image low (szkic) dokłada przedmioty** (wazon, kostka). Finał high z dopiskiem „no
  furniture, no vases, no objects”.
- **Kling ma tylko 16:9, 9:16 i 1:1.** Do kadru 3:4 (hero) zrobiłem wersję 1:1 (GPT Image „widen to a
  square, extend only wall and floor”), bo pudełko ~0,9 przycina kwadrat najmniej.
- **Tripo 3D:** bryła komody wierna, tył i boki dorysowane. GLB ma 68 MB, a pokazanie go wymaga
  biblioteki 3D (~250 kB) i `blob:` w CSP, więc pilot nie trafił na stronę. Pakshot 3/4 na jednolitym
  tle to najlepsze wejście.
- **Pula boost:** po jej wyczerpaniu serwis odrzucił zadanie `not_enough_boost_credits`. Ponowienie
  poszło już z salda głównego.

### Szablony promptów (sprawdzone)

```text
# Pętla światła (Kling FLF, start = koniec = zdjęcie)
Tripod-locked camera, perfectly still, one continuous shot. <Pora dnia/miejsce>: soft sunlight from
the <side> slowly drifts across the wall and the floor, <jedna rzecz, która się rusza: leaf shadows /
the sheer curtain barely stirs / fine dust floats in the beam>, then the light settles back to exactly
the first frame. Every piece of furniture stays perfectly still and identical. Locked white balance,
no flicker, no camera movement, no people, no text.

# Klatka startowa „z korytarza” (GPT Image, ref = prawdziwe zdjęcie pokoju)
Pull the camera back: the same <room> photographed from the hallway outside it, through a wide open
doorway in a warm white limewashed wall. A pale oak door frame borders the view on the left, right and
top; the room seen through the doorway is exactly this photograph, every piece identical in shape,
colour and place, same light. The hallway foreground is empty: light oak floor and bare walls only,
no furniture, no objects. 35mm lens, eye level, centered, calm. No text, no logos, no people.

# Wejście (Kling FLF: start = klatka z korytarza, koniec = prawdziwe zdjęcie)
One single continuous shot, no cuts, no dissolves. The camera glides slowly and steadily straight
forward through the wide oak doorway into the <room>, the door frame passing out of view at the edges,
and settles exactly on the final frame. Constant slow speed, gentle ease at the end. Every piece stays
identical and in place: <wymień meble>. No people, no text, no camera shake.

# Zmiana światła (GPT Image, ref = zdjęcie)
Relight this exact photograph only: the same room, same camera, same composition, every object
identical in shape, colour and position. Change the time of day to <blue hour before sunrise /
sunset with a long amber band of low light on the wall>. Photographic, natural. No text, no people.

# Makro materiału (GPT Image, bez ref)
Macro photograph of <material>: close-up of <faktura>, soft raking morning sunlight from the left,
warm tones, shallow depth of field. Just the material filling the frame, no object, no furniture
shape, no text, no logos, no people.
```

Wspólne zasady: styl domu zawsze w tych samych słowach (ciepła biel, jasny dąb, len, światło z lewej,
obiektyw 35 mm). Wymieniaj meble z nazwy („Every piece stays identical: the bed, the dresser…”), a
„no text, no logos, no people” dawaj zawsze.

---

## 4. Proces jednego assetu

```
cena (generate cost) → szkic/tanio → arkusz klatek → akceptacja albo poprawka promptu
→ finał → pobranie → kodowanie + plakat → wpis w media-log → kod → zrzuty → commit
```

**Zawsze `--wait --json > plik.json`.** URL wyniku wyciągniesz regexem
(`https:[^"]+hf_[^"]+\.(png|mp4|glb)`). W JSON-ie jest też URL wejścia, więc szukaj `hf_`.

```bash
# Arkusz klatek: 1 klatka/s, 10 kafli
ffmpeg -v error -y -i clip.mp4 -vf "fps=1,scale=240:-2,tile=5x2" -frames:v 1 sheet.jpg
# Gęsto wokół podejrzanego miejsca (przenikanie?)
ffmpeg -v error -y -ss 6.8 -i clip.mp4 -t 1.4 -vf "fps=6,scale=200:-2,tile=8x1" -frames:v 1 cut.jpg
# Dokładnie ostatnia klatka (do seam-locka albo porównania ze zdjęciem)
ffmpeg -v error -y -i clip.mp4 -vf reverse -frames:v 1 last.png
```

**Log mediów** (`docs/cinematic/media-log*.md`): numer, asset, model, ustawienia, cennik, saldo po
zadaniu, wynik (użyty/odrzucony i dlaczego). Saldo sprawdzaj co kilka zadań, bo cennik nie zawsze
równa się temu, co faktycznie schodzi.

---

## 5. Kodowanie (ffmpeg)

| Zastosowanie | Komenda (istotne flagi) | Typowy rozmiar |
| --- | --- | --- |
| Odtwarzanie (pętla, film raz) | `-an -c:v libx264 -preset slow -crf 24–26 -pix_fmt yuv420p -g 48 -movflags +faststart` | 0,35–1,1 MB na 5–10 s przy 960–1440 px |
| Scroll-scrub | jw. + `-g 6 -keyint_min 6 -sc_threshold 0` (albo `-g 12` dla długich) | 1,4 MB na 6 s; ogród z liśćmi: 8,9 MB przy GOP 6 (!) |
| Liście i trawy | podnieś CRF do 29–31 | 2,3 MB zamiast 8,9 |
| Plakat | `-frames:v 1 -c:v libwebp -q:v 80 -compression_level 6` | 20–60 kB (las: 160 kB) |
| Proporcje jak zdjęcie | `scale=1440:804,setsar=1` (2000×1116 → 1,792) | rozciągnięcie ~1% jest niewidoczne |
| Sklejenie części | `concat` w `filter_complex`, druga część bez 1. klatki (`trim=start_frame=1`) | – |

**Gotchas:**
- **Wysokość musi być parzysta** (`1280:715` daje błąd).
- **Plakat rób z surowego pliku, nie z zakodowanego MP4**, i zawsze z tej samej klatki, od której
  startuje wideo (kadr musi się zgadzać 1:1).
- **`object-cover` przytnie tak samo**, gdy obraz i wideo mają ten sam środek i proporcje ograniczone
  szerokością. Kadr 3:4 i 2:3 tego samego zdjęcia przy tym samym środku dają identyczny wygląd w
  pudełku ~0,9.

---

## 6. Wzorce w kodzie, które się sprawdziły (React)

Wszystko w `src/components/cinematic/` + `src/lib/motion.ts` (flagi i zapytania mediów jako stałe).

| Komponent | Co robi | Kluczowe decyzje |
| --- | --- | --- |
| `AuthFilm` | film na panelu ze zdjęciem (logowanie/rejestracja) gra raz, potem przenika w zdjęcie | tylko `lg`, raz na kartę (`sessionStorage`), `play().catch` → zdjęcie, „dojazd” CSS do `transform-origin` detalu |
| `SplitText` | słowa wjeżdżają spod linii | `sr-only` z całym zdaniem + `aria-hidden` dla słów; `<em>` zostaje wokół swoich słów; `::first-letter` nie działa w środku → wielka litera w JS |
| `HeroLight` | hero LCP + pętla po pierwszej interakcji | `scroll/pointermove/touchstart/keydown` `once`; `playbackRate 0.7`; scena poranna/wieczorna wg motywu |
| `AmbientLoop` | plakat → pętla przy zbliżeniu do ekranu, pauza poza nim | `IntersectionObserver` z marginesem; `phones`, `once` (zostaje na ostatniej klatce); flaga per użycie |
| `ScrollFilm` | czas klipu = postęp scrolla nad prawdziwym zdjęciem | seek dopiero po `seeked` (bez kolejki); `trigger="arrival"` dla zdjęcia na górze strony; `pin` (sticky + tor N×100vh) dla długich filmów; fokus klawiatury odsłania zdjęcie |
| `useSmoothScroll` | Lenis | dynamiczny import po starcie; tylko `(pointer: fine)`; `stop()` gdy Radix ustawia `data-scroll-locked` na `<body>` |

**Odkrycia przy wkładaniu klipów do kodu:**
- **CSP:** `blob:` nie jest dozwolony w `media-src` (spada na `default-src 'self'`), więc wideo idzie
  zwykłym `src`. Zakres (Range) z nginx i Vite wystarcza do seekowania.
- **Lighthouse:** wideo nie może być w ścieżce LCP. Interakcja albo IntersectionObserver jako
  bramka. Plakat w nakładce scroll-scrubu dostaje `loading="lazy"`, bo inaczej waży w pierwszym
  ładowaniu strony głównej.
- **Autoplay może być odrzucony** (oszczędzanie baterii). Zawsze `play().catch(() => fallback)`, inaczej
  zasłona zostaje na pierwszej klatce.
- **Film raz na kartę:** klucz `film-seen:<url klipu>` w `sessionStorage`, wszystko w try/catch.
- **Hotspoty pod filmem:** nakładka `pointer-events` i fokus wewnątrz → `setFocused(true)`
  odsłania zdjęcie, żeby punkty nie były niedostępne z klawiatury.
- **Przyklejony pasek z drugim przyciskiem „Add to bag”:** gdy schowany, używaj `invisible`
  (`visibility: hidden`), wtedy znika z drzewa dostępności i `getByRole` w e2e nie trafia dwa razy.
  Chowaj go też przy stopce.
- **View Transitions:** nazwa (`view-transition-name`) może stać na jednym elemencie naraz. Helper
  przenosi ją na kliknięty element tuż przed nawigacją, a obrazy o różnych proporcjach potrzebują
  `object-fit: cover` w `::view-transition-old/new`.
- **Lenis vs. dialogi:** Lenis przewija `html`, a Radix blokuje `body`, więc strona pod oknem
  wyszukiwania dalej się przewijała. Obserwuj atrybut `data-scroll-locked`.
- **Ciemny motyw** ze stanu Reduxa + `matchMedia('(prefers-color-scheme: dark)')` z nasłuchem zmian
  dla trybu „system”.
- **Teksty:** każda nowa treść (opisy materiałów, kroki „las → warsztat”) bez twierdzeń, których
  aplikacja nie może potwierdzić. Zmieniłem „by hand, one piece at a time” na „in their own
  workshops”.

---

## 7. Weryfikacja: skrypty i narzędzia

**Zrzuty w czasie** (wideo wymaga prawdziwego Chrome):

```js
// shots.mjs: node shots.mjs <url> <prefix> <W>x<H> [reduce] [ms,ms,...] chrome
import { chromium } from '@playwright/test';
const [url, out, size = '1440x900', reduce = 'no', times = '400', channel = ''] = process.argv.slice(2);
const [width, height] = size.split('x').map(Number);
const browser = await chromium.launch(channel ? { channel } : {});
const page = await browser.newPage({ viewport: { width, height }, reducedMotion: reduce === 'reduce' ? 'reduce' : 'no-preference' });
await page.goto(url, { waitUntil: 'load' });
let last = 0;
for (const t of times.split(',').map(Number)) {
  await page.waitForTimeout(t - last); last = t;
  console.log(t, await page.evaluate(() => { const v = document.querySelector('video'); return v ? `t=${v.currentTime.toFixed(2)} ended=${v.ended}` : 'no video'; }));
  await page.screenshot({ path: `${out}-${t}.png` });
}
await browser.close();
```

**Pełna strona:** przewiń 30 × 0,8 ekranu (żeby załadowały się leniwe obrazy i Reveal), wróć na górę,
`fullPage: true`. Do przeglądu wielu stron naraz: miniatury 240 px sklejone `hstack` (skrypt
`audit.sh`).

**Zrzut za logowaniem lub płatnością:** tymczasowy spec w `e2e/` korzystający z helperów projektu
(rejestracja → koszyk → zamówienie → `pay('4242…')`). Po zrzucie usuń plik.

**Emulator Androida:**

```bash
SDK="$LOCALAPPDATA/Android/Sdk"
"$SDK/emulator/emulator.exe" -avd Pixel_API36 -no-snapshot-save -no-boot-anim &   # potem: adb wait-for-device, sys.boot_completed
adb reverse tcp:4173 tcp:4173      # podgląd buildu (Vite dev słucha na ::1, a adb łączy na 127.0.0.1: użyj `vite preview --host 127.0.0.1`)
adb reverse tcp:5000 tcp:5000      # gateway/API
adb reverse tcp:10000 tcp:10000    # zdjęcia (Azurite)
adb shell am start -a android.intent.action.VIEW -d "http://localhost:4173/" com.android.chrome
adb exec-out screencap -p > shot.png
adb shell input swipe 540 1900 540 500 600     # przewijanie
adb forward tcp:9222 localabstract:chrome_devtools_remote   # Chrome emulatora przez Playwright connectOverCDP
```

Pułapki:
- **Lokalny `.env.production`** wpisał do buildu adres API z Azure, więc emulator nie miał danych.
  Rozwiązanie: build w trybie bez tego pliku (`vite build --mode compose`; nazwa `local` jest
  zastrzeżona).
- **Ciemny motyw:** emulator ma go domyślnie włączonego, a to dobry test.
- **Lighthouse w Dockerze** (Chrome na Windows nie startował): `docker run --network host` obraz
  Playwrighta + `CHROME_PATH` + `npx @lhci/cli autorun` na przebudowanym kontenerze UI (nginx + gzip,
  czyli realne wagi).
- **Testy wizualne** (Storybook) w obrazie Playwrighta mają `reducedMotion: 'reduce'`. Efekty za
  reduced motion nie ruszyły żadnego snapshotu.
- **e2e:** pojedyncze testy (płatność, kod rabatowy) raz się wysypały po długim przebiegu i przeszły
  przy powtórce. Powtórz przed szukaniem winy w nowym kodzie.

---

## 8. Rytm sesji, który się sprawdził

1. **Start:** `account status`, budżet = saldo − próg, rezerwa ~20%. Gałąź `feat/cinematic`.
   Research w tle (subagent). Przegląd skilli.
2. **Audyt aplikacji:** pełne zrzuty każdej strony na 1440 i 390 px (oraz na emulatorze). Tak
   znalazłem m.in. plakietki pod sercem, jedną kolumnę kart na telefonie, stopkę wyższą niż ekran i
   brak paska zakupu.
3. **Plan z kosztorysem** (`PLAN.md`): ulepszenia po kolei, każde z ceną. Najpierw te bez kredytów.
4. **Pilot taniego potoku** (2–3 zadania): sprawdź, czy tańszy model wystarcza, zanim rozpiszesz resztę
   na drogi.
5. **Po każdym ulepszeniu:** lint, testy, zrzuty desktop/mobile/reduced motion, wpis w media-logu,
   commit z opisem dla ludzi (bez ID z audytu).
6. **Koniec:** build, budżet bundla, pełne e2e, testy wizualne, Lighthouse, emulator i raport (co
   zrobione + zrzuty, jak wyłączyć każdy efekt, kredyty z logu i `transactions`, co nie wyszło,
   co dalej).

---

## 9. Zasady z briefu, które warto przenieść 1:1

- **Git:** praca na osobnej gałęzi, małe commity, bez pusha. Bez dotykania backendu i
  infrastruktury. Pliki właściciela zostają nieskomitowane.
- **Media bez ludzi, tekstu, logo i marek.** Tekst zawsze w HTML.
- **Produkty na sprzedaż się nie zmieniają.** Rusza się kamera i światło, mebel zostaje identyczny.
- **Surowe pliki w `media-work/`** (ignorowane przez git). Gotowe w `src/assets/cinematic/`.
- **Każdy efekt za flagą.** Reduced motion → statycznie. Mobile → lżej albo statycznie.
- **Jakość:**
  - Lighthouse: perf ≥ 0,9, LCP ≤ 2,5 s, CLS ≤ 0,1, TBT ≤ 200 ms, ≤ 1,5 MB na mierzoną stronę.
  - Nowe biblioteki tylko, jeśli są warte rozmiaru. 3D tylko leniwie.
  - Snapshoty aktualizuj tylko dla celowo zmienionych ekranów.

---

## 10. Wyniki z tego projektu (punkt odniesienia)

| | Runda 1 | Runda 2 |
| --- | --- | --- |
| Generacje | 13 (4 obrazy, 9 wideo) | 44 (+1 odrzucona przez serwis) |
| Kredyty | 460,5 | 130,25 z salda + 183,5 z puli boost |
| Główny model wideo | Seedance 2.5 (draft → finał) | Kling O3 FLF |
| Efekty | film na logowaniu/rejestracji, SplitText, Lenis, hero ze światłem, wejście do lookbooka | 404, puste stany, wieczorne hero, wejścia do 4 lookbooków, okładki kolekcji i twórców, strona Materiały, paczka po zapłacie, film na O nas |
| Lighthouse (5 stron) | perf 0,98–0,99, LCP ~1 s | perf 0,98–0,99, LCP ~1 s |
| Bundle główny | 420 → 423 kB (z 500) | 428 kB |

Szczegóły: `docs/cinematic/REPORT.md`, `REPORT-2.md`, `media-log.md`, `media-log-2.md`, zrzuty w
`shots/` i `shots-2/`.
