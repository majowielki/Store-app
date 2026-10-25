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
