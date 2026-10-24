# Plan: kinowe ulepszenie sklepu

Gałąź `feat/cinematic` (od `main`). Saldo na starcie 1088, próg 400, **budżet 688 kredytów**;
rezerwa na poprawki ~20% (≈140), więc plan celuje w ≤ 550. Inspiracje: [inspiration.md](inspiration.md),
log każdej generacji: [media-log.md](media-log.md).

## Zasada przewodnia

Wideo z Higgsfield jest warstwą pod spodem, a tekst i ruch są w kodzie na wierzchu (jak u Jacka Robertsa).
Ruch ma być wolny i ciągły, jak kamera na statywie albo spokojny krok w głąb pokoju. Bez cięć, bez gadżetów
pod kursor (magnetyczne przyciski, pierścienie, marquee, tilt 3D są wykluczone).

Klatka końcowa każdego klipu to **prawdziwe zdjęcie produktu** z `Blobs/`, więc ujęcie kończy się dokładnie
na tym, co sklep sprzedaje. Generowany jest tylko początek (drzwi, światło przed świtem), na którym nie ma
żadnego towaru.

Każdy efekt ma flagę w jednym pliku `UI/store-app.UI/src/lib/motion.ts`. Przy `prefers-reduced-motion`
nic się nie odtwarza i niczego się nie pobiera. Mobile dostaje lżejszy wariant albo obraz statyczny.

## Ulepszenia

| # | Ulepszenie | Gdzie | Technika | Kredyty (szac.) |
| --- | --- | --- | --- | --- |
| 1 | **Wejście do domu** na ekranie logowania i **pokój, który się budzi** na rejestracji | `AuthShell`, `/login`, `/register` (panel od `lg`) | Logowanie: drzwi się otwierają, kamera wchodzi do sypialni i zatrzymuje się na komodzie z lampą (koniec = `StonewareDrumTableLamp-1`). Rejestracja: niebieska godzina przechodzi w poranne słońce, kamera stoi (koniec = `StonewashedLinenThrow-1`). Klip gra raz, potem wolne przenikanie w detal (`sign-in.webp` / `register.webp`), a cytat wchodzi słowo po słowie. Przy kolejnej wizycie w sesji od razu widać detal. | ~230 (zrobione: 229,75) |
| 2 | **Wejście tekstu** (w stylu SplitText) w nagłówkach sekcji | `SectionTitle`, nagłówki strony głównej, cytat w `AuthShell` | Własny lekki komponent: słowa w maskach, wjazd od dołu ze staggerem po wejściu w widok; `aria-label` z pełnym zdaniem. Bez GSAP. | 0 |
| 3 | **Płynne przewijanie (Lenis)** | cała witryna sklepu (bez panelu admina) | `lenis` (~5 kB gz), ładowany po starcie i tylko przy precyzyjnym wskaźniku, wyłączony przy reduced motion; kotwice i `scrollTo` z routera dalej działają. | 0 |
| 4 | **Żywe światło w hero** strony głównej | `Hero` (zdjęcie `hero1.webp`) | Klip z pętlą (start = koniec = `hero1`): słońce i cienie liści wolno przesuwają się po ścianie, sofa stoi. LCP zostaje obrazem; wideo pobiera się dopiero po pierwszej interakcji (scroll, ruch myszy, dotyk), tylko na desktopie i bez Save-Data. | ~95 (szkic 15 + finał 80) |
| 5 | **Wejście do pokoju w lookbooku** | nagłówek `LookPage` (`/looks/:slug`) albo `LookbookFeature` | Krótki najazd kamery (start = prawdziwe zdjęcie lookbooka) sterowany scrollem (scroll-scrub, GOP 4–8); po dojściu do końca zdjęcie z punktami produktów. Leniwie, poza budżetem Lighthouse (lookbook nie jest mierzony). | ~95, robione tylko, jeśli zostanie budżet ponad rezerwę |

Suma planu: ~420 kredytów, rezerwa ≥ 140, saldo końcowe docelowo ~600–660 (minimum 400).

## Kolejność i kontrola

1. Ulepszenie 1, potem 2 i 3 (kod), potem 4, a 5 tylko przy wolnym budżecie.
2. Po każdym: `npm run lint`, `npm test`, zrzuty Playwright (1440 px, 390 px, reduced motion), commit.
3. Na koniec: `npm run build`, `npm run check:bundle`, e2e zmienionych stron, `npm run test:visual`
   (snapshoty tylko celowo zmienionych ekranów) i raport [REPORT.md](REPORT.md).

## Ryzyka

- **Morfing towaru w klipie.** Klatki końcowe to zdjęcia produktów, a w środku ujęcia sprawdzam arkusz
  klatek; jeśli mebel „pływa”, klip odpada.
- **Waga strony głównej.** Wideo hero nie jest w ścieżce LCP i nie ładuje się bez interakcji, więc
  Lighthouse (który nie scrolluje) go nie pobiera.
- **Lenis vs. zachowanie routera.** Przywracanie scrolla i skip link sprawdzam w e2e.
