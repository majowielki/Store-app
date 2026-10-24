# Inspiracje: ruch „cinematic” dla sklepu (Japandi / skandynawski)

Data researchu: 2026-10-24. Kierunek: ciepła biel, jasny dąb, len, terakota, miękkie światło dzienne.
Ruch ma być spokojny, premium i w duchu „slow living”, bez efekciarstwa.

**Gust właściciela**
- Lubi: Lenis (smooth scroll) oraz wejścia nagłówków i tekstu w stylu GSAP SplitText.
- Nie lubi: magnetic buttons, kółek podążających za kursorem, pasków marquee/ticker i 3D tilt na zdjęciach.

> Zastrzeżenie metodologiczne: strony Awwwards podają opis, tagi, technologie i listę wyróżnionych
> elementów („Loader”, „Lookbook” itd., każdy z nagraniem), ale **nie opisują ruchu słowami**.
> Techniki poniżej wywnioskowałem z tagów, technologii i tych elementów. Nie oglądałem stron na żywo,
> więc przed decyzją warto przejrzeć każdą przez 2–3 minuty.

---

## 1. Awwwards: wybrane strony

Legenda dopasowania: ✔ pasuje do gustu · ~ częściowo (wziąć jeden element) · ✘ raczej odpadnie.

| # | Strona | Wyróżnienie | Technika ruchu (wg tagów i elementów) | Dopasowanie |
|---|---|---|---|---|
| 1 | **Oakame**, meble z odzyskanego dębu (FR). [awwwards](https://www.awwwards.com/sites/oakame), [beaucoup.studio](https://beaucoup.studio/en/projects/oakame/) | SOTD 1.11.2024 + Developer Award | loader, home slider, układ lookbooka, mikrointerakcje, konfigurator sofy; paleta #F6F1EB / #403A34 | ✔ najbliższy nam klimat (dąb, złamana biel); wzór dla lookbooka i loadera |
| 2 | **Props Furniture** (Studio Lumio). [awwwards](https://www.awwwards.com/sites/props-furniture), [props.studiolumio.com](https://props.studiolumio.com/) | SOTD 16.05.2023 + Dev | preloader, lookbook, przejścia, parallax, galeria; tag „Gestures / Interaction” | ✔ preloader i przejścia; ~ sprawdzić, czy „gestures” to nie efekty kursora |
| 3 | **Loam House**, nieruchomość (Melbourne). [awwwards](https://www.awwwards.com/sites/loam-house), [residences.loamhouse.com.au](https://residences.loamhouse.com.au/) | HM 24.09.2026 | „chaptered scroll journey”: scroll podzielony na rozdziały, GSAP, parallax, przejścia, storytelling, typografia; paleta krem / oliwkowy brąz | ✔ wzór na „pokoje jako rozdziały” i lookbook |
| 4 | **Belgrade Arbor**, architektura. [awwwards](https://www.awwwards.com/sites/belgrade-arbor), [belgradearbor.rs](https://belgradearbor.rs/en) | SOTD 17.10.2026 + Dev | intro animation, smooth scroll, sekcje z wideo, „3D experience” (Next.js, GSAP, Three.js); Animations 8.0/10 | ~ wziąć intro i smooth scroll z wideo; ✘ pominąć ciężkie 3D |
| 5 | **Sobha Privy Collection**, luksusowe nieruchomości. [awwwards](https://www.awwwards.com/sites/sobha-privy-collection) | SOTD 23.09.2026 + Dev | „Zoom on scroll” (wjazd kamerą w kadr), parallax, galerie i mapa w WebGL (Three.js) | ~ zoom-on-scroll to dobry wzór na „wejście do pokoju”; ✘ galerie WebGL są za głośne |
| 6 | **Mejuma Tuscany**, domy w Toskanii. [awwwards](https://www.awwwards.com/sites/mejuma-tuscany), [mejuma-tuscany.com](https://mejuma-tuscany.com/) | Nominee 8.10.2026 | loader, animowana nawigacja, duże zdjęcia tła, GSAP + Webflow; osobny element „Mouse hover” | ✔ loader i spokojne pełnoekranowe kadry; ~ hover może być kursorowy |
| 7 | **Halcyon Lighting**, oświetlenie, Shopify. [awwwards](https://www.awwwards.com/sites/halcyon-lighting-2), [halcyon.co.nz](https://halcyon.co.nz/) | Nominee 4.10.2026 | minimalizm, scroll, typografia, **tryby Day & Night** | ✔ pomysł „pora dnia” pasuje do naszej narracji o świetle |
| 8 | **Kvell**, meble w stylu skandynawskim (Locomotive). [case study](https://2022.locomotive.ca/en/work/kvell) | SOTD + Developer (wg wyników wyszukiwania) | studio znane z płynnego scrolla (locomotive-scroll); szczegóły niezweryfikowane, bo case study się nie załadowało | ~ do obejrzenia: najbliższa nam kategoria produktowa |
| 9 | **Studio Iro**, wnętrza i architektura. [awwwards](https://www.awwwards.com/inspiration/homepage-studio-iro), [studioiro.co](https://studioiro.co/) | element inspiracji | „Opening animation” (sekwencja intro), galeria projektów | ✔ wzór krótkiego, cichego intro |
| 10 | **Æbele Interiors** (Exo Ape). [awwwards](https://www.awwwards.com/inspiration/minimal-style-scroll-navigation-aebele-interiors), [aebeleinteriors.com](https://aebeleinteriors.com/) | element inspiracji | minimalistyczna nawigacja przy scrollu (tagi: minimal, clean, scrolling) | ✔ nawigacja, która nie przeszkadza |
| 11 | **Terreno Hotel**, Palma. [awwwards](https://www.awwwards.com/sites/https-www-terrenohotel-com), [terrenohotel.com](https://www.terrenohotel.com/en/) | Nominee 22.10.2026 | strona prowadzona fotografią; ruch nieopisany | ~ referencja dla fotografii, nie dla ruchu |
| 12 | **TWODESIGNERS**, belgijskie studio wnętrz. [awwwards](https://www.awwwards.com/TWODESIGNERS/) | profil z SOTD/SOTM (nie wiem, za który projekt) | niezweryfikowane | ? do przejrzenia |

Listy kategorii do dalszego przeglądania:
[interior-design](https://www.awwwards.com/websites/interior-design/) ·
[architecture](https://www.awwwards.com/websites/architecture/) ·
[furniture](https://www.awwwards.com/websites/furniture/) (głównie starsze HM, poza Props i Oakame mało ruchu wartego uwagi).

**Co się powtarza u najlepszych dopasowań:**
- Krótki loader albo intro, które przechodzi płynnie w hero, zamiast osobnego ekranu z licznikiem procentów.
- Smooth scroll i strona dzielona na rozdziały (Loam House, Belgrade Arbor).
- Pełnoekranowe zdjęcie lub wideo z dużą typografią na wierzchu.
- Lookbook jako osobny, wolniejszy układ (Oakame, Props).
- Paleta 2-kolorowa: złamana biel + ciemny brąz. To praktycznie nasza.

**Czego unikać, choć na Awwwards to częste:**
- Galerie i mapy w WebGL 3D.
- Efekty hover i „gestures” sterowane kursorem.
- Zoom i parallax na każdym zdjęciu.
- Moja ogólna obserwacja trendu (nie z tych stron): magnetic buttons, kursory-pierścienie i marquee to dziś standard na Awwwards, więc trzeba je świadomie odfiltrować.

---

## 2. Filmy YouTube

**Czego nie udało się sprawdzić:** YouTube nie zwrócił opisów ani transkrypcji; potwierdziłem tylko tytuły i autorów przez oEmbed.
Poniższe streszczenia opierają się na opisie i znacznikach czasu z [videohighlight.com](https://videohighlight.com/), które **nie zawierają pełnej transkrypcji**.
Nie mogę więc potwierdzić szczegółów technicznych pokazanych na ekranie (np. czy jest scroll-scrub klatek, jakie easingi).

### 2a. „Claude Motion Design Just Destroyed Website Builders ($10,000 Websites)”, Jack Roberts
[youtube.com/watch?v=fq3wx-AR6B0](https://www.youtube.com/watch?v=fq3wx-AR6B0) · [streszczenie opisu](https://videohighlight.com/v/fq3wx-AR6B0)

Workflow w trzech poziomach, wg opisu:
1. **Referencje i generowanie.** Najpierw zbiera wzorce z Refero i Mobbin. Potem podłącza Higgsfield do Claude przez MCP, żeby Claude sam generował obrazy i wideo.
2. **Ruch w kodzie nad wideo.** Wygenerowane wideo jest bazą, a nad nim leży warstwa motion graphics w kodzie: pętle, „żelkowe” przyciski, grywalna strona 404, animowana postać.
   - Pokazuje widok „X-ray”, który rozdziela, co jest nagraniem, a co kodem.
   - To potwierdza technikę „wideo pod spodem + ruch i tekst w kodzie na wierzchu”.
3. **Backend.** Tracker agencji na MongoDB (materiał sponsorowany, dla nas nieistotny).

Morał: obie warstwy trzymać osobno, żeby wideo i animacje UI dało się poprawiać niezależnie.
Z tego filmu bierzemy zasadę warstw, a nie „żelkowe” przyciski, bo to ten sam rodzaj zabawy co magnetic buttons.

### 2b. „How to Build Stunning 3D Websites with Claude Opus 5.5 (Full Tutorial)”, kanał *Create a Pro Website*
[youtube.com/watch?v=fSFrHISdpt8](https://www.youtube.com/watch?v=fSFrHISdpt8) · [streszczenie opisu](https://videohighlight.com/v/fSFrHISdpt8)

Kroki wg opisu:
1. Projekt w Claude Opus.
2. Brand kit wygenerowany przez AI.
3. Hero image, a z niego kinowe hero video w Higgsfield (image-to-video).
4. Domena i hosting (Hostinger, linki afiliacyjne).
5. Claude buduje interaktywną stronę „3D” z modelem roweru i filmem hero, z pomocą pobieralnego pliku skill.
6. Poprawki krótkimi promptami, potem publikacja.

Nie da się potwierdzić, czy „3D” to Three.js/WebGL, czy przewijane klatki wideo.

### 2c. Kontekst: tak opisują ten workflow inne tutoriale (zweryfikowane teksty)

**Klatka startowa i końcowa → wideo → klatki pod scroll** ([Chase AI](https://chaseai.io/blog/claude-code-nano-banana-2-3d-scroll-websites)):
- Generuje się kadr startowy (2K+, tło zgodne z sekcją), a z niego jako referencji kadr końcowy.
- Model wideo (Kling 3.0 / Veo 3.1) dostaje oba kadry jako first/last frame.
- FFmpeg wycina ok. 180 klatek do WebP.
- Klatki rysuje się na `<canvas>` jak flipbook zależny od pozycji scrolla.
- Preload: najpierw pierwsze klatki, potem reszta przed wejściem w sekcję.

**Scrub `video.currentTime`** ([tutorial Higgsfield MCP](https://skills.tenten.co/original-html/claude-code-scroll-video-higgsfield-tutorial.html)):
- Sekcja ma ok. 300vh, wideo jest przypięte (sticky 100vh, `object-fit: cover`).
- GSAP ScrollTrigger z `scrub: 0.5`; timeline tworzony po `loadedmetadata`. Lenis opcjonalnie.
- W promptach do generowania: wolny, ciągły ruch i statyczna kamera, żeby wideo czysto się przewijało.
- Plik: ok. 1920 px, H.264 CRF 26, bez audio, < 5 MB, plus `poster`.
- Na mobile sekwencja klatek albo statyczny obraz; przy `prefers-reduced-motion` tylko poster.

**Gotowe skille:**
- Higgsfield wydał skill „Motion Website Generator” dla Claude ([AlphaSignal](https://alphasignal.ai/news/higgsfield-ships-a-claude-skill-that-builds-animated-websites-automatically), [YouMind](https://youmind.com/landing/x-viral-articles/motion-website-playbook-higgsfield-claude)). Generuje klipy przez MCP, wyciąga klatki i składa stronę z GSAP ScrollTrigger + Lenis. Efekty: grain, winieta, tinty, „scroll pacing”.
- Społecznościowe repo [scroll-cinematic-claude](https://github.com/zubair-trabzada/scroll-cinematic-claude).

---

## 3. Wnioski dla naszego sklepu

- **Ekran logowania: „wejście do pokoju”.**
  - Krótki klip 3–5 s z image-to-video: kadr startowy (próg lub przedpokój w świetle dnia) → kadr końcowy (salon, który zostaje tłem). Wolny dolly-in, statyczna ekspozycja, bez cięć.
  - Logowanie nie ma scrolla, więc klip gra raz (`autoplay muted playsinline`) i zatrzymuje się na ostatniej klatce. Ta klatka jest też posterem i fallbackiem.
  - Formularz i nagłówek (SplitText, linie z dołu, stagger ok. 0.06 s) wchodzą w ostatniej sekundzie klipu.
  - Przy kolejnej wizycie w sesji pokazujemy od razu kadr końcowy.

- **Hero na stronie głównej.**
  - Poster jako LCP (`<img fetchpriority="high">` lub `poster`), wideo dociągane dopiero po LCP lub w idle.
  - Do wyboru: cicha pętla ambient (firanka, przesuwające się światło, 6–10 s, < 3 MB) albo krótka sekcja scroll-scrub 200–300vh, ale tylko jedna na stronę.
  - Nagłówek wchodzi przez SplitText po linii. Dla czytelności ciepły scrim (gradient w kolorze lnu), nie czarny.

- **Lookbook (wzór: Loam House, Oakame).**
  - Rozdziały-pokoje i odsłanianie zdjęć maską `clip-path: inset()` od dołu przy wejściu w viewport.
  - Delikatny parallax obrazu w ramce (5–8%, bez skalowania ponad 1.05) i podpisy wchodzące liniami.
  - Lenis zsynchronizowany ze ScrollTrigger: `lenis.on('scroll', ScrollTrigger.update)` + `gsap.ticker`.

- **Przejścia między pokojami i kategoriami.**
  - Cross-fade lub krótkie przejście maską (300–500 ms, ease-out), np. przez View Transitions API przy nawigacji routera. Wzorem jest „Zoom on scroll” z Sobha, ale w wersji spokojnej.
  - Bez przejęcia scrolla na listach produktów, w koszyku i w checkoucie: tam ruch tylko w mikroskali.

- **Wydajność.**
  - Wideo ładować leniwie przez IntersectionObserver, pauzować poza ekranem i szanować `Save-Data`. Formaty: MP4 H.264 + WebM/AV1.
  - Sekwencje klatek tylko dla krótkich sekcji (≤ 120 klatek WebP, w 2 rozdzielczościach). Na słabym mobile statyczny obraz.
  - Budżet ok. 5 MB mediów „cinematic” na stronę, mierzony Lighthouse jak w etapie 4.

- **Dostępność i reduced motion.**
  - Przy `prefers-reduced-motion: reduce` wyłączamy Lenis i scrub, pokazujemy poster, a tekst bez podziału na znaki.
  - SplitText z zachowaniem `aria-label` (czytnik ma dostać całe zdanie). Wideo dekoracyjne z `aria-hidden`.

- **Workflow.**
  - Klipy generujemy z własnych zdjęć produktów i wnętrz (kadr startowy i końcowy), z promptem „slow, continuous, locked camera, soft daylight”.
  - Wideo i UI trzymamy jako osobne warstwy (zasada z filmu Robertsa), żeby dało się podmienić klip bez ruszania kodu.

- **Czego unikać.**
  - Magnetic buttons, kursory-pierścienie, marquee, 3D tilt, „żelkowe” przyciski.
  - Galerie WebGL 3D, długie preloadery z licznikiem, które blokują zakupy.
  - Tekst na ruchliwym wideo bez scrimu, wiele scroll-scrubów na jednej stronie i parallax na każdym zdjęciu.
