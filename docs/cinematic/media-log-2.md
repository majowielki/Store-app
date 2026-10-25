# Log mediów Higgsfield: runda 2

Saldo na starcie: **2427,5** (plan Ultra), próg **1500** (hook: `HF_CREDIT_FLOOR=1500` w
`.claude/settings.local.json`), budżet **927,5**. Surowe pliki: `UI/store-app.UI/media-work/r2/` (poza gitem).
„Cennik” to wynik `higgsfield generate cost`; „Saldo po” to `higgsfield account status` po zadaniu.

**Odkrycie:** na planie Ultra `kling_o3_flf` i `gpt_image_2_5` nie zdejmują kredytów: saldo stało na
2427,5 po sześciu takich generacjach, choć cennik podaje 1,5–12,5. Płatny jest Seedance (sprawdzę przy
pierwszym użyciu w tej rundzie).

| # | Asset | Model | Ustawienia | Cennik | Saldo po | Wynik |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Pilot: pętla światła w sypialni | `kling_o3_flf` | pro, 10 s, 16:9, start = koniec = `Blobs/Lookbook-Bedroom.webp` | 12,5 | 2427,5 | zaakceptowany jako dowód potoku: meble stoją idealnie, światło subtelne (1928×1076) |
| 2 | 404: pusty pokój | `gpt_image_2_5` | high, 2k, 16:9, „pusty pokój, bez mebli i przedmiotów” | 2,75 | 2427,5 | **użyty** |
| 3 | 404: pętla firanki i światła | `kling_o3_flf` | pro, 10 s, start = koniec = #2 | 12,5 | 2427,5 | **użyty** → `src/assets/cinematic/empty-room.mp4` (1440 px, CRF 26, 0,67 MB); firanka faluje mocniej, niż prosiłem, ale pokój zostaje pusty |
| 4 | Pusty koszyk | `gpt_image_2_5` | high, 1k, 3:2, ref #2 | 1,5 | 2427,5 | **użyty** → `empty-cart.webp` |
| 5 | Pusta lista ulubionych | `gpt_image_2_5` | high, 1k, 3:2, łukowa wnęka, ref #2 | 1,5 | 2427,5 | **użyty** → `empty-wish.webp` |
| 6 | Puste porównanie | `gpt_image_2_5` | high, 1k, 3:2, dwie wnęki, ref #2 | 1,5 | 2427,5 | **użyty** → `empty-compare.webp` |
| 7 | Hero wieczorem (ciemny motyw) | `gpt_image_2_5` | high, 2k, 1:1, zmiana światła na `hero1` (przycięte do 3:4) na zachód słońca + poszerzenie do kwadratu | 2,75 | 2427,5 | **użyty** → `hero-evening.webp` (1200 px); meble zgodne z `hero1` |
| 8 | Hero wieczorem: pętla | `kling_o3_flf` | pro, 10 s, 1:1, start = koniec = #7 | 12,5 | 2427,5 | **użyty** → `hero-evening.mp4` (960 px, CRF 24, 0,59 MB) |

Poprawka jakości: ffmpeg ignoruje `-quality` w `libwebp`, przez co plakaty i kadry z obu rund miały
3–10 kB i były rozmyte. Wszystkie są przekodowane z surowych plików przez `-q:v 80 -compression_level 6`
(20–60 kB).
| 9 | Lookbook sypialnia: z korytarza | `gpt_image_2_5` | high, 2k, 16:9, edycja `Lookbook-Bedroom.webp` | 2,75 | 2427,5 | **użyty** |
| 10 | Lookbook sypialnia: wejście | `kling_o3_flf` | pro, 10 s, start #9, koniec `Lookbook-Bedroom.webp` | 12,5 | 2427,5 | **użyty** → `look-bedroom.mp4` (1280 px, 0,91 MB); ostatnia klatka praktycznie równa zdjęciu, więc Kling zastąpił planowany Seedance (~117) |
| 11 | Lookbook kuchnia: z korytarza | `gpt_image_2_5` | high, 2k, 16:9, edycja `Lookbook-Kitchen.webp` | 2,75 | 2427,5 | **użyty** (drzwi trochę z boku: kamera dochodzi łukiem) |
| 12 | Lookbook pokój dziecka: z korytarza | `gpt_image_2_5` | high, 2k, 16:9, edycja `Lookbook-KidsRoom.webp` | 2,75 | 2427,5 | **użyty** |
| 13 | Lookbook ogród: z domu przez szklane drzwi | `gpt_image_2_5` | high, 2k, 16:9, edycja `Lookbook-Garden.webp` | 2,75 | 2427,5 | **użyty** |
| 14 | Lookbook kuchnia: wejście | `kling_o3_flf` | pro, 10 s, start #11 | 12,5 | 2427,5 | **użyty** → `look-kitchen.mp4` (0,9 MB) |
| 15 | Lookbook pokój dziecka: wejście | `kling_o3_flf` | pro, 10 s, start #12 | 12,5 | 2427,5 | **użyty** → `look-kids-room.mp4` (0,81 MB) |
| 16 | Lookbook ogród: wyjście na taras | `kling_o3_flf` | pro, 10 s, start #13 | 12,5 | 2427,5 | **użyty** → `look-garden.mp4` (CRF 29, 2,27 MB: liście ważą) |
| 17 | Kolekcja Warm Minimal: pętla poranka | `kling_o3_flf` | pro, 10 s, start = koniec = `Collection-WarmMinimal.webp` | 12,5 | 2427,5 (pula boost) | **użyty** → `collection-warm-minimal.mp4` (1280 px, 0,44 MB) |
| 18 | Kolekcja Small Spaces: pętla południa | `kling_o3_flf` | jw., `Collection-SmallSpaces.webp` | 12,5 | 2427,5 (boost) | **użyty** (0,53 MB) |
| 19 | Kolekcja Outdoor Season: pętla zachodu | `kling_o3_flf` | jw., `Collection-OutdoorSeason.webp`, trawy w wietrze | 12,5 | 2427,5 (boost) | **użyty** (CRF 31, 0,82 MB) |
| 20 | Twórca Artifex: warsztat | `kling_o3_flf` | jw., `Maker-Artifex.webp`, pył w smudze światła | 12,5 | 2427,5 (boost) | **użyty** (0,56 MB) |
| 21 | Twórca Comfora: pracownia tapicerska | `kling_o3_flf` | jw., `Maker-Comfora.webp` | 12,5 | 2427,5 (boost) | **użyty** (0,35 MB) |
| 22 | Twórca Homestead: warsztat | `kling_o3_flf` | jw., `Maker-Homestead.webp` | 12,5 | 2427,5 (boost) | **użyty** (0,66 MB) |
| 23 | Twórca Luxora: atelier | `kling_o3_flf` | jw., `Maker-Luxora.webp` | 12,5 | 2427,5 (boost) | **użyty** (0,5 MB) |
| — | Twórca Modenza: pierwsza próba | `kling_o3_flf` | jw. | — | — | **odrzucone przez serwis**: `not_enough_boost_credits` |
| 24 | Materiały: dąb, makro (test puli) | `gpt_image_2_5` | high, 2k, 16:9 | 2,75 | **2424,75** | pierwsze zadanie z głównej puli (do strony „Materiały”) |
| 25 | Twórca Modenza: warsztat stolarski | `kling_o3_flf` | jw., `Maker-Modenza.webp` | 12,5 | 2412,25 | **użyty** (0,62 MB) |

**Wyjaśnienie puli:** `higgsfield account transactions` pokazuje, że upgrade dał osobne **200 kredytów
„Concurrent Boost”**. Z nich szły zadania #1–#23 (183,5 kr), dlatego saldo główne stało na 2427,5. Po ich
wyczerpaniu serwis odrzucił zadanie (`not_enough_boost_credits`), a od #24 zadania schodzą z głównego salda
zgodnie z cennikiem.
| 26–31 | Materiały: orzech, len, bouclé, trawertyn, rattan, kamionka (makro) | `gpt_image_2_5` | high, 2k, 16:9, „sam materiał, bez przedmiotu” | 6 × 2,75 | 2395,75 | **użyte** (z #24: 7 kadrów) |
| 32–38 | Materiały: 7 pętli światła | `kling_o3_flf` | pro, **5 s**, start = koniec = kadr makro | 7 × 6,25 | 2352 | **użyte** → `material-*.mp4` (1280 px, 0,43–0,64 MB) i `material-*.webp` |
| 39 | Pilot 3D: komoda 8-szufladowa | `tripo_h3_1_image_to_3d` | detailed geometry + texture, PBR, z `Blobs/8DrawerDresser-3.webp` | 18 | 2334 | **niepublikowany**: bryła wierna (8 szuflad, mosiężne uchwyty, cokół), boki i tył dorysowane; GLB 68 MB; publikacja wymaga kompresji, `<model-viewer>` (~250 kB) i `blob:` w CSP nginx, czego brief nie pozwala ruszać; zrzuty: `shots-2/H-3d-pilot-dresser.jpg` |
| 40 | Potwierdzenie zamówienia: paczka w lnie | `gpt_image_2_5` | high, 2k, 16:9, „pusta zawieszka, bez napisów” | 2,75 | 2331,25 | **użyty** |
| 41 | Potwierdzenie: poranne światło na paczce | `kling_o3_flf` | pro, 5 s, tylko klatka startowa #40 | 6,25 | 2325 | **użyty** → `order-parcel.mp4` (0,34 MB), gra raz i zostaje na ostatniej klatce |
