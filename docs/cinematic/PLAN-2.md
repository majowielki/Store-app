# Plan: druga runda

Na gałęzi `feat/cinematic`, w ciągu kolejnych commitów, po rundzie 1 ([REPORT.md](REPORT.md)).
Inspiracje: [inspiration.md](inspiration.md) (sekcja „Runda 2”). Audyt stron zrobiłem 25.10.2026:
zrzuty desktop 1440 i mobile 390 wszystkich stron treści i sklepu (`UI/store-app.UI/media-work/audit/`).

## Budżet

| | Kredyty |
| --- | --- |
| Saldo (25.10, plan Ultra, wygasa za ok. 2 tygodnie) | 2427,5 |
| Próg na koniec | **1500** |
| Budżet | **927,5** |
| Rezerwa na poprawki (~20%) | 185 |
| **Plan** | **≤ 740** |

**Krok 0:** w hooku ustawię `HF_CREDIT_FLOOR=1500` (komenda hooka w `.claude/settings.local.json`), żeby
blokował wszystko, co zeszłoby poniżej 1500, a nie poniżej 400 jak w rundzie 1.

## Czego nauczyła runda 1

- **Klatka końcowa:** ujęcie kończy się na prawdziwym zdjęciu produktu, generuje się tylko początek. Dzięki
  temu meble się nie zmieniają, a przejście do zdjęcia z punktami jest bez skoku.
- **Koszt finału:** Seedance 2.5 to szkic za 15–30, a finał ze szkicu zawsze 1080p (6 s = 96, 8 s = 128).
  Koszt sprawdzam zawsze z `--draft_job_id`.
- **Lighthouse:** wideo na mierzonych stronach ładuje się dopiero po interakcji albo po scrollu, a LCP
  zostaje obrazem.
- **Klawiatura:** fokus odsłania od razu to, co zasłania film.

## Nowe możliwości Higgsfield (wycenione 25.10)

| Narzędzie | Do czego | Koszt |
| --- | --- | --- |
| `kling_o3_flf` (klatka pierwsza + ostatnia) | tanie pętle „zmienia się tylko światło” | **6,25** za klip |
| `kling3_0` pro, 10 s | dłuższe, spokojne ujęcia | 25 |
| `seedance_2_5` 1080p | ruch kamery przez pokój (sprawdzony) | 96–160 + szkic |
| `nano_banana_2_relight` | to samo zdjęcie w innym świetle (wieczór, lampa) | 2 |
| `image_decompose` | rozbicie zdjęcia pokoju na warstwy do paralaksy | 2 |
| `image_background_remover` | wycięcie mebla z tła | 1 |
| `tripo_h3_1_multiview_to_3d` | model 3D (GLB) ze zdjęć produktu | 9 |
| `gpt_image_2_5` high 2k | klatki startowe, puste wnętrza, makro materiałów | 2,75 |
| `seed_audio` | dźwięk tła | 0,1 (**odrzucam**: dźwięk w sklepie to gadżet) |

**Pilot na start (ok. 25 kr):** dwie pętle Kling O3 FLF (hero wieczorem, makro dębu) obok jednej Kling 3.0
pro. Jeśli jakość wystarczy, wszystkie pętle „tylko światło” idą tanim potokiem (6–25 zamiast 114), a
Seedance zostaje dla ujęć z ruchem kamery. Jeśli nie wystarczy, pętle z punktów D–G zastąpię zdjęciami
z przesuwającym się w CSS światłem, a kredyty pójdą tylko na 404, wieczór, lookbooki i paczkę.

## Ulepszenia

Kolejność realizacji. Każde dostaje przełącznik w `src/lib/motion.ts`, zrzuty 1440/390/reduced motion,
testy i commit.

### A. Bez kredytów: układ, tekst, interakcje

1. **Pasek „Add to bag” przyklejony do dołu** na stronie produktu (miniatura, wykończenie, cena, przycisk),
   gdy główny przycisk zniknie z ekranu. Na telefonie siedzi nad dolnym dokiem. Teraz po przewinięciu do
   recenzji trzeba wracać na górę.
2. **„You may also like” na telefonie** jako dwie kolumny albo poziomy rząd zamiast kolumny wielkich kart.
3. **Artykuł w journalu jak strona książki:** węższa kolumna, zdjęcia odsłaniane maską od dołu, cienka
   linia postępu czytania i duża okładka „Następny artykuł” na końcu.
4. **Stopka na telefonie:** grupy linków w akordeonach. Dziś stopka zajmuje ponad ekran i na krótkich
   stronach przytłacza treść.
5. **Puste stany i 404 bez przeprosin:** „Ta strona się przeprowadziła albo nigdy tu nie mieszkała.” plus
   linki do pokoi. Serce ulubionych wypełnia się od dołu.
6. **Przejście okładki** kolekcji, lookbooka i kafla pokoju w nagłówek strony docelowej (View Transitions,
   jak zdjęcie produktu w rundzie 1).
7. **Odcień pory dnia (opcjonalnie):** tło i nakładki pod tekstem lekko cieplejsze rano i wieczorem, wg
   zegara przeglądarki, z wyłącznikiem.

### B. 404 i puste stany: „pokój, którego jeszcze nie urządziliśmy” (~45 kr)

- **404:** pusty pokój bez żadnych mebli (biała ściana, dąb, okno). Wygenerowana klatka (2,75) i pętla
  światła z kurzem w smudze słońca (Kling FLF ~6–25, awaryjnie Seedance 114).
- **Puste koszyk, ulubione i porównanie:** po jednym cichym kadrze (pusta półka, wnęka w świetle), 3 × 2,75.

### C. Wieczór w ciemnym motywie (~30, awaryjnie ~115)

- **Wieczorny salon:** `relight` zdjęcia hero (2), czyli ten sam salon wieczorem przy lampie.
- **Pętla:** płomień światła lampy i zmierzch za oknem (Kling FLF albo Seedance).
- **Kiedy widać:** w ciemnym motywie hero pokazuje wieczór zamiast poranka.
- **Wyłącznik:** dla właściciela `motionFlags.eveningHero`.

### D. Pozostałe lookbooki: wejście do pokoju (~160)

- **Nowe ujęcia:** sypialnia, kuchnia, pokój dziecka i ogród dostają wejście jak salon w rundzie 1
  (klatka z korytarza lub furtki, koniec na zdjęciu lookbooka).
- **Gdzie:** na stronie `/looks/:slug` (nagłówek), a strona główna pokazuje ujęcie pierwszego lookbooka.
- **Kredyty:** sypialnia na Seedance (~117, najważniejsza), trzy pozostałe Kling FLF (~3 × 15). Jeśli
  Kling nie utrzyma mebli, tylko sypialnia.

### E. Kolekcje o swojej porze dnia (~45)

- **Pora dnia:** „Warm Minimal” to poranek, „Small Spaces” południe, „Outdoor Season” wieczór.
- **Pętla:** każda okładka dostaje spokojną pętlę światła (start = koniec = okładka, Kling FLF).
- **Wejście:** okładka przechodzi w hero kolekcji (A6).

### F. Twórcy: warsztat (~140)

- **Ujęcia:** każdy z 5 twórców dostaje 2–3 krótkie ujęcia warsztatu bez ludzi (wióry na stole, światło
  w pyle, kawałek drewna w imadle, krosno z lnem). Klatki 2,75, klipy Kling.
- **Układ:** strona twórcy z kolumną klipów przyklejoną z boku, które przenikają się przy przewijaniu
  kroków „What they make / Why we work with them”.
- **Lista twórców:** jako rozdziały (sticky stack).

### G. Nowa zakładka „Materiały” i makro na stronie produktu (~110)

- **Strona `/materials`** (w menu Inspiration): dąb, orzech, len, bouclé, trawertyn, rattan, kamionka.
  Każdy materiał dostaje makro w ruchomym świetle (klatka 2,75 + Kling ~12,5), krótki tekst o
  pielęgnacji (link do „Caring for oak”) i link do katalogu z tym wykończeniem.
- **Te same pętle na stronie produktu:** przy wyborze wykończenia, w miejscu „Materials” w specyfikacji.
  Treść jest nowa, więc piszę ją ja (bez marek i bez obietnic, których sklep nie składa).

### H. Strona produktu: „z każdej strony” (pilot, ~45)

- **Pilot:** 3 produkty flagowe (sofa, fotel, komoda) przez `tripo_h3_1_multiview_to_3d` z ich zdjęć
  (3 × 9, plus poprawki).
- **Warunek publikacji:** model trafia do sklepu tylko, jeśli kształt i kolor zgadzają się ze zdjęciami,
  bo sklep nie może pokazać innego mebla, niż sprzedaje.
- **Przeglądarka:** `<model-viewer>` ładowany dopiero po kliknięciu „Obejrzyj z każdej strony”, poza
  pomiarem Lighthouse.

### I. Potwierdzenie zamówienia: paczka w lnie (~30)

- **Klip:** 3–4 s, paczka owinięta lnem i sznurkiem na dębowym blacie, światło się ociepla, ostatnia
  klatka zostaje.
- **Gdzie:** na stronie po opłaceniu zamówienia.
- **Nawiązanie:** domyka historię z ekranu logowania.

### J. O nas: „Od lasu do pokoju” (opcjonalnie, ~240)

- **Klip w dwóch częściach:** dębowy las o poranku, warsztat z deskami i finalny pokój (koniec na zdjęciu
  sklepu), przewijany w rytm scrolla jak lookbook.
- **Kiedy:** tylko jeśli po A–I zostanie ponad rezerwę.

## Suma

| | Wariant tani potok (Kling działa) | Wariant bez Kling |
| --- | --- | --- |
| Pilot | 25 | 25 |
| B 404 + puste stany | 25 | 125 |
| C wieczór | 30 | 115 |
| D lookbooki | 160 | 120 (tylko sypialnia) |
| E kolekcje | 45 | 0 (CSS) |
| F twórcy | 140 | 15 (same kadry) |
| G materiały | 110 | 20 (same kadry) |
| H 3D | 45 | 45 |
| I paczka | 30 | 115 |
| **Razem** | **~610** | **~580** |
| J „Od lasu do pokoju” | +240, jeśli zostanie | — |

W obu wariantach saldo końcowe to ≥ 1500 + rezerwa (~1800), a hook z progiem 1500 pilnuje reszty.

## Kontrole na koniec (jak w rundzie 1)

- **Testy:** `npm run lint`, `npm test`, `npm run build`, `npm run check:bundle`, e2e całości na stosie
  z Dockera, `npm run test:visual` (snapshoty tylko celowo zmienionych ekranów).
- **Lighthouse:** w Dockerze na 5 mierzonych stronach.
- **Emulator:** przegląd na emulatorze Androida.
- **Raport:** `REPORT-2.md`.
