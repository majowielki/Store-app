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

---

## Runda 2 (2026-10-25)

Cel tej rundy: podstrony i wzorce, których jeszcze nie ruszaliśmy (karta produktu, kategorie, kolekcje, makers, journal, about, 404, koszyk, potwierdzenie zamówienia, wishlist).
Zrobione w rundzie 1 i tu **nie wracamy**: film logowania, film rejestracji („świt wypełnia pokój”), nagłówki słowo po słowie, Lenis, hero z pętlą światła na ścianie, scrub „przez drzwi do salonu” w lookbooku, statyczny pasek ogłoszeń, dwie kolumny produktów na telefonie.
Stan kodu sprawdzony przed pisaniem: przejście zdjęcia z listy do karty produktu już istnieje (`Link viewTransition` w `ProductCard.tsx`, `ProductsList.tsx` i `ProductGallery.tsx`), swatche wykończeń też, a w `package.json` jest Lenis i React Router 7, ale nie ma GSAP.

> Zastrzeżenie jak w rundzie 1: Awwwards podaje nazwy elementów („Sticky Bottom Content”, „404 Page”) i nagrania, ale ruchu nie opisuje słowami.
> Techniki poniżej wywnioskowałem z nazw elementów, tagów i technologii. Nagrań nie oglądałem; zrobiłem ok. 35 wyszukiwań i pobrań.

### R2.1 Awwwards: nowe strony (podstrony, nie strony główne)

| # | Strona | Wyróżnienie | Co podejrzeć (technika wg elementów i tagów) | Dopasowanie |
|---|---|---|---|---|
| 1 | **Auwa**, japońska marka lifestyle'owa. [awwwards](https://www.awwwards.com/sites/auwa), [auwa.life](https://auwa.life) | HM 16.05.2026 (More Air) | strona jako redakcyjny „dom” dla journala, sklepu i historii; elementy: Loading, „Book” (interakcja i układ), układ artykułu i journala; Next.js + Tailwind; paleta #F8F7F4 / #0F1623 | ✔ wzór dla `/journal` i `/journal/:slug`; ton „kokoro” bardzo blisko Japandi |
| 2 | **Blå Station**, szwedzkie meble. [awwwards](https://www.awwwards.com/sites/bla-station), [blastation.com](https://blastation.com) | HM 2.07.2026 (Intervaro) | meble podane jak magazyn; elementy: „Smooth Image Pagination” (płynna paginacja zdjęć) i „Sticky Bottom Content” (przyklejona treść u dołu ekranu) | ✔ najbliższa kategoria; paginacja galerii i przyklejony pasek zakupu na karcie produktu |
| 3 | **For Living**, wykończenia wnętrz i meble na wymiar (Mediolan). [awwwards](https://www.awwwards.com/sites/for-living), [for-living.it](https://www.for-living.it/) | HM 12.07.2026 (Forwwward) | GSAP + Webflow + After Effects; paleta #9C4827 (terakota) / #23281D; elementy: hero wideo, „Approach steps” i „Approach cascade” (kroki procesu wchodzące kaskadą), scroll transition, product grid, „Next case study” | ✔ kroki procesu dla `/makers/:slug` i `/about`; ✘ pominąć „Map hover” i „Hover parallax” |
| 4 | **Arteriors**, oświetlenie i meble. [awwwards](https://www.awwwards.com/sites/arteriorshome-com), [arteriorshome.com](https://www.arteriorshome.com/) | Nominee 30.05.2026 (Magebit) | „editorial commerce”: kinowe zdjęcia, galeria inspiracji, osobno nagrany buying flow, lista produktów i karta produktu; Magento + Tailwind | ~ wzór spięcia galerii inspiracji ze sklepem i nagrany flow zakupu; ✘ infinite scroll |
| 5 | **CANCAN Furnishings**, meble (Singapur). [awwwards](https://www.awwwards.com/sites/cancan-furnishings), [cancanfurnishings.com](https://cancanfurnishings.com/) | HM 16.10.2025 + E-commerce Honors (360&5) | Shopify + Contentful; elementy: Collection Page (sofy), galeria na karcie produktu, „Product Discovery” pod produktem; showroom marki pokazuje całe aranżacje zamiast rzędów mebli ([SquareRooms](https://squarerooms.com.sg/?p=31158)) | ✔ układ strony kolekcji i blok „odkryj więcej” na karcie produktu |
| 6 | **Ferm Living**, duński homeware. [awwwards](https://www.awwwards.com/sites/ferm-living), [fermliving.com](https://fermliving.com) | HM 10.08.2026 (Signifly) | redesign na Shopify, hasło „comfort and accessible luxury”; tagi clean, minimal, photo & video; brak wyróżnionych elementów | ✔ najbliższy asortyment; referencja dla fotografii i spokojnej karty produktu, ruch prawie zerowy |
| 7 | **dsgn Interior**, szwedzkie studio wnętrz. [awwwards](https://www.awwwards.com/sites/dsgn-interior), [dsgninterior.se](https://dsgninterior.se) | HM 7.09.2026 (Studio Fables) | GSAP + vanilla JS; pełnoekranowe projekty, płynne przejścia stron (osobny element „Page transitions”), parallax; [archiwum](https://dsgninterior.se/en/dsgn-archive) mebli z odzysku jako numerowane karty „Archive #NNN” z polami Obiekt / Ilość / Stan / Cena i znacznikiem „Sold” | ✔ przejścia między podstronami; format „karty katalogowej” dla edycji limitowanych lub `/makers` |
| 8 | **Smeulders Interieurgroep**, wnętrza premium (NL). [awwwards](https://www.awwwards.com/sites/smeulders-interieurgroep), [smeulders-ig.nl](https://smeulders-ig.nl/) | Nominee 14.07.2026 (Comaxx) | WordPress + GSAP; opowieść o rzemiośle i zrównoważeniu, duże tła i wideo, strona projektu | ✔ struktura `/about`: rzemiosło → materiał → ludzie; ruch nieopisany |
| 9 | **Studio Dado**, projektowanie wnętrz hotelowych. [awwwards](https://www.awwwards.com/sites/studio-dado), [studiodado.com](https://www.studiodado.com) | SOTD 25.02.2026 + Dev 7.49 (Motto) | paleta #FFFFFF / #5D5346 (ciepły brąz); elementy: system logo, Contact, „Interaction”; Animations 7.40 | ✔ paleta i strona kontaktu; ~ sprawdzić, czy „Interaction” nie jest kursorowe |
| 10 | **Lion Fabrics**, tkaniny obiciowe. [awwwards](https://www.awwwards.com/sites/lion-fabrics), [lion-fabrics.com](https://www.lion-fabrics.com) | HM 15.02.2024 (Bornfight) | Shopify; własny konfigurator do szczegółowego oglądania tkaniny, układ poziomy, tagi 3D i gestures | ~ wziąć ideę „obejrzyj materiał z bliska”; ✘ układ poziomy i gesty |
| 11 | **Furniture Presence**, cyfrowe showroomy mebli. [awwwards](https://www.awwwards.com/sites/furniture-presence), [textura.works](https://textura.works/) | HM 19.08.2024 (Textura) | elementy: „Furniture AR”, „Furniture 3D Configurator”, tagi 360 i storytelling | ~ referencja dla „mebel w twoim pokoju” (AR); ✘ pełny konfigurator 3D |
| 12 | **ROOOF**, luksusowe meble dla psów. [awwwards](https://www.awwwards.com/sites/rooof-pet-furniture), [shoprooof.com](https://www.shoprooof.com/) | HM 4.11.2025 (League) | wyróżniony element „404 Page”, mikrointerakcje, After Effects | ~ obejrzeć tylko 404 |

Na marginesie, do 2-minutowego przejrzenia:
- [Furnistør](https://www.awwwards.com/sites/furnistor) (HM 1.09.2025): skandynawskie demo motywu WooCommerce z nagranym koszykiem i stroną „About & Team”. To szablon, nie marka, więc liczy się tylko układ.
- [Ascensión Latorre](https://www.awwwards.com/sites/ascension-latorre) (SOTD, meble robione ręcznie przez rzemieślników): daty nie sprawdzałem.
- [Coutumes](https://www.awwwards.com/sites/coutumes) (HM 5.09.2026): ma „Custom Product”, ale też typografię zmienną pod kursorem (✘).

**Co się powtarza w tej rundzie:**
- „Editorial commerce”: journal, inspiracje i sklep to jedna opowieść (Auwa, Blå Station, Arteriors).
- Treść przyklejona u dołu ekranu zamiast pływających przycisków (Blå Station).
- Proces i rzemiosło jako numerowane kroki wchodzące kaskadą (For Living, Smeulders).
- Kolekcja pokazana jako urządzony pokój, a nie siatka (CANCAN).
- Osobno nagrane przejścia stron: to dziś element, za który się nagradza (dsgn Interior).

**Czego nie znalazłem:** zwycięzcy SOTD 2025–2026 w meblach z widokiem 360°/3D na karcie produktu.
Poza Halcyon (runda 1) nie znalazłem też żadnej strony z trybem dnia i nocy. 3D pojawia się głównie jako konfiguratory (Lion Fabrics, Furniture Presence) i mapy osiedli ([Belle Oaks](https://www.awwwards.com/sites/belle-oaks-r)), czyli rzeczy dla nas za ciężkie.

### R2.2 Filmy i źródła o Higgsfield w webie

**Dwa filmy właściciela.** Treść opisów streściłem w rundzie 1 (§2a, §2b) i nie powtarzam jej.
Tym razem znów potwierdziłem tylko tytuł i autora przez oEmbed (fSFrHISdpt8: kanał *Create a Pro Website*). Wyszukiwanie omówień filmu Jacka Robertsa nie dało nic nowego.
**Nadal niezweryfikowane:** to, co jest pokazane na ekranie (czy „3D” to WebGL, czy klatki wideo; jakie easingi i długości), bo transkrypcje są niedostępne.

**Nowe, praktyczne ustalenia:**
- **Higgsfield MCP → After Effects** ([blog Higgsfield](https://higgsfield.ai/blog/MCP-for-motion-designers), [AlphaSignal](https://alphasignal.ai/news/higgsfield-lets-claude-build-fully-editable-after-effects-projects)).
  - Claude woła ponad 30 modeli obrazu i wideo, składa kompozycję i oddaje edytowalny projekt AE.
  - Dla nas to opcja na dopracowanie klipu (tint, grain, kadrowanie) bez ruszania kodu, ale wymaga subskrypcji AE.
  - Kwoty zarobków w tytule wpisu to marketing.
- **Higgsfield 3D (GLB).** Sprawdziłem w lokalnej dokumentacji skilla `higgsfield-generate` (`references/model-catalog.md`), bez uruchamiania CLI.
  - Model „Multi-Image to 3D” (silnik Meshy) bierze 1–4 zdjęcia (przód, bok, tył, detal), opcjonalnie teksturuje i zwraca siatkę w stylu GLB.
  - Kosztu w kredytach nie sprawdzałem. Publicznych tutoriali „Higgsfield GLB na karcie produktu” nie znalazłem.
- **Wyświetlanie GLB: `<model-viewer>`** ([web.dev](https://web.dev/model-viewer/), [przykłady ładowania](https://modelviewer.dev/examples/loading/)).
  - Z `poster` i odroczonym ładowaniem model nie pobiera się, dopóki użytkownik go nie wywoła; poster zostaje do końca ładowania.
  - Draco opłaca się dopiero, gdy oszczędność przekracza rozmiar dekodera (ponad 100 KB).
  - AR na iOS (Quick Look) wymaga drugiego pliku USDZ.
  - Komponent sam wstrzymuje render poza ekranem (IntersectionObserver).
- **Turntable bez 3D** ([RotaKit](https://ja.wordpress.org/plugins/?p=339259), [Framer Turntable 360](https://www.framer.com/marketplace/components/turntable-360/)).
  - Sekwencja co najmniej 36 klatek (co 10°), przeciąganie z inercją, strzałki z klawiatury, opcjonalnie obrót związany ze scrollem.
  - Pionowy swipe ma dalej przewijać stronę.
  - Klatki da się wyciąć z wideo.
- **Orbita generowana AI** ([Dreamina](https://dreamina.capcut.com/ai-video/orbit-shot-ai-video-generator), [eachlabs: Kling vs Seedance](https://www.eachlabs.ai/blog/kling-vs-seedance-which-model-for-which-shot)).
  - W promptach przyjęło się: produkt na środku matowego blatu, kamera wolno okrąża, miękkie światło na krawędziach.
  - Kling jest polecany do jednego produktu, który ma wyglądać identycznie w każdym ujęciu; Seedance do spójności między wieloma ujęciami.
  - O bezszwowej pętli 360° źródła milczą. Moja ocena: przy pełnym obrocie AI łatwo zmienia geometrię mebla (liczbę nóg, łączenia), więc bezpieczniejszy jest łuk 90–120°.
- **„Exploded view” i scroll po klatkach** ([Webflow: AirPods z GSAP](https://webflow.com/made-in-webflow/website/gsap-video-animation-on-scroll), [Codrops: warstwowy zoom](https://tympanus.net/codrops/2025/10/29/building-a-layered-zoom-scroll-effect-with-gsap-scrollsmoother-and-scrolltrigger/)).
  - Tutoriali o rozłożonych meblach nie ma. Technika jest ta sama co w rundzie 1: klatki na canvas, a cały ciąg to jeden timeline z jednym triggerem.
  - Dla mebli realniejsza wersja: warstwy PNG (blat, nogi, okucia) rozsuwane kodem zamiast generowanego wideo. Konstrukcja zostaje wtedy prawdziwa.
- **Pora dnia** ([localghost.dev](https://localghost.dev/blog/time-based-background-colour-transitions-with-temporal-and-css-color-mix/), [circadian/sunCalc](https://shawnfromportland.substack.com/p/create-time-of-day-responsive-color)).
  - Kolor tła liczony z lokalnej godziny przez CSS `color-mix()`.
  - Wersja z prawdziwym wschodem słońca wymaga lokalizacji; u nas wystarczy zegar przeglądarki, bez geolokalizacji.

### Wnioski na rundę 2

Legenda: **media** = co trzeba wygenerować, **kod** = tylko frontend; wysiłek S (≤ 0,5 dnia), M (1–2 dni), L (3+ dni lub pilotaż).

1. **Karta produktu: materiał z bliska.**
   - Przy każdym wykończeniu (swatche już są) cicha makro-pętla 4–6 s: słoje dębu, po których przesuwa się światło, splot lnu, faktura terakoty.
   - Wybór swatcha podmienia pętlę crossfadem ok. 400 ms. Pętla ładuje się dopiero po pierwszym kliknięciu, a przy reduced motion zostaje sama tekstura.
   - *Media:* wideo (image-to-video z istniejących tekstur). *Wysiłek:* M, 1 klip na wykończenie.
2. **Karta produktu: przyklejony pasek zakupu u dołu** (wzór Blå Station).
   - Pojawia się, gdy główny przycisk „Do koszyka” zniknie z ekranu: miniatura, nazwa, wykończenie, cena, przycisk. Wjeżdża od dołu (200 ms, ease-out); na telefonie na pełną szerokość.
   - *Kod.* *Wysiłek:* S.
3. **Karta produktu: obejrzyj z każdej strony.**
   - Wariant A: GLB z Higgsfield Multi-Image to 3D w `<model-viewer>` za posterem, ładowany na kliknięcie, bez auto-rotate i bez tilt, z AR na Androidzie.
   - Wariant B (bezpieczniejszy): łuk 90–120° jako wideo przewijane przeciąganiem.
   - *Media:* 3D lub wideo. *Wysiłek:* L. Najpierw pilotaż na 1 produkcie z oceną wierności bryły.
4. **Karta produktu: „w skali pokoju”.**
   - Przełącznik zdjęcie studyjne ↔ ten sam mebel w urządzonym pokoju.
   - Linie wymiarów (szer. × gł. × wys.) rysują się w SVG przez `stroke-dashoffset`, kiedy przełącznik jest aktywny.
   - *Media:* obraz (aranżacja z produktem). *Wysiłek:* M.
5. **Kolekcje i kategorie: kolekcja jako pokój** (wzór CANCAN).
   - `/collections/:slug` otwiera się cichą pętlą „pokoju” w porze dnia przypisanej kolekcji (poranek, południe, wieczór).
   - Okładka z listy kolekcji przechodzi w hero tym samym `viewTransition`, który już działa dla produktów.
   - Na liście produktów przy zmianie kategorii lub filtra karty pojawiają się z opóźnieniem 30–40 ms każda, bez przejmowania scrolla.
   - *Media:* wideo na kolekcję plus kod. *Wysiłek:* M.
6. **Makers: kroki warsztatu** (wzór For Living „Approach steps / cascade”).
   - `/makers/:slug` ma przyklejoną kolumnę z krótkimi klipami procesu (heblowanie, olejowanie, tkanie), które zmieniają się crossfadem, gdy tekst kroków przewija się obok. Numery kroków wchodzą słowo po słowie.
   - *Media:* wideo, 3–4 klipy (mogą być wspólne dla makerów jednego rzemiosła). *Wysiłek:* M–L.
7. **About: od deski do krzesła** (struktura Smeulders: rzemiosło → materiał → ludzie).
   - Jedyny scrub na tej stronie: „rozłożony” mebel składa się podczas przewijania.
   - Lepiej z warstw PNG rozsuwanych kodem niż z generowanego wideo, bo AI zmyśla łączenia.
   - *Media:* obrazy warstw (albo wideo first/last frame). *Wysiłek:* L.
8. **Journal: artykuł jak książka** (wzór Auwa).
   - Wąska kolumna i inicjał; zdjęcia na pełną szerokość odsłaniane `clip-path` od dołu.
   - Postęp czytania jako cienka lniana linia u góry.
   - Na końcu duża okładka „następny artykuł” (jak „Next case study” u For Living), która przechodzi w hero kolejnego wpisu przez `viewTransition`.
   - *Kod.* *Wysiłek:* S–M.
9. **404: pokój, którego jeszcze nie urządziliśmy.**
   - Przygaszona pętla: światło przesuwa się po pustej ścianie, w kadrze stoi jedno krzesło.
   - Nagłówek słowo po słowie i trzy linki (kolekcje, nowości, journal). Bez grywalnej 404 z filmu Robertsa: to zabawa nie w tym tonie.
   - *Media:* wideo (można wziąć klip z hero i dać inny kadr). *Wysiłek:* S.
10. **Koszyk, pusty koszyk i wishlist.**
    - Puste stany z jednym spokojnym obrazem (pusta dębowa półka w porannym świetle) zamiast ikony.
    - Serce wishlisty wypełnia się od dołu (`clip-path`, 300 ms), jakby nalewało się światło.
    - Licznik koszyka raz delikatnie pulsuje, a szuflada koszyka wysuwa się z ease-out. Bez latającej miniatury i bez sprężyn.
    - *Media:* 1–2 obrazy plus kod. *Wysiłek:* S.
11. **Potwierdzenie zamówienia: paczka w lnie.**
    - Po płatności (`/orders/:id`) klip 3–4 s gra raz i staje na ostatniej klatce: dłonie owijają paczkę lnem i sznurkiem na dębowym stole.
    - „Dziękujemy” wchodzi słowo po słowie. To klamra z filmem logowania; przy kolejnym wejściu na zamówienie od razu ostatnia klatka.
    - *Media:* wideo. *Wysiłek:* S–M.
12. **Globalnie: pora dnia** (Halcyon z rundy 1 + technika z localghost.dev).
    - Tło, scrim pod tekstem i odcień posterów przesuwają się o kilka procent między porankiem, południem i wieczorem według zegara przeglądarki (`color-mix()`, zmienne CSS).
    - Ręczny przełącznik w stopce, bez geolokalizacji. Przy reduced motion zmiana jest skokowa, bez animacji.
    - *Kod.* *Wysiłek:* S–M.

**Kolejność, jaką bym proponował:** najpierw 2, 8, 10 i 12 (sam kod, szybki efekt), potem 1, 9 i 11 (krótkie klipy, mało ryzyka), na koniec 5 i 6.
Punkty 3, 4 i 7 dopiero po pilotażu na jednym produkcie, bo zależą od wierności generowanej bryły i od budżetu kredytów (próg 400).

**Czego z tej rundy nie bierzemy:**
- Typografia zmienna pod kursorem (Coutumes), map hover i hover parallax (For Living).
- Infinite scroll (Arteriors), mapy i konfiguratory 3D (Belle Oaks, Furniture Presence), układ poziomy z gestami (Lion Fabrics), grywalna 404.
