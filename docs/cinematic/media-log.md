# Log mediów Higgsfield

Każda próba po kolei. Saldo na starcie: **1088** kredytów, próg **400**, budżet **688**.
Surowe pliki: `UI/store-app.UI/media-work/` (poza gitem). Koszty z `higgsfield generate cost` i salda przed/po.

| # | Asset | Model | Ustawienia | Koszt | Wynik |
| --- | --- | --- | --- | --- | --- |
| 1 | Logowanie: klatka startowa (drzwi), szkic | `gpt_image_2_5` | low, 1k, 4:3, ref `sign-in.webp` (paleta) | 0,25 | odrzucony: wazon i kostka, których sklep może nie sprzedawać; posłużył jako referencja dla #2 |
| 2 | Logowanie: klatka startowa (drzwi) | `gpt_image_2_5` | high, 2k, 4:3, ref #1, „puste wejście” | 2,75 | **użyty** (`media-work/door-final.png`) |
| 3 | Logowanie: wideo wejścia, szkic 1 | `seedance_2_5` | draft, 5 s, 4:3, omni_reference, start #2, koniec `sign-in.webp` | 15 | odrzucony: ok. 3,4 s przenikanie zamiast jednego ruchu, wymyślona sofa i stolik |
| 4 | Logowanie: wideo wejścia, szkic 2 | `seedance_2_5` | draft, 6 s, 4:3, start #2, koniec `Blobs/StonewareDrumTableLamp-1.webp` (prawdziwe zdjęcie produktu) | 18 | zaakceptowany: jeden ciągły ruch, lampa i komoda zgodne ze zdjęciem |
| 5 | Logowanie: wideo wejścia, finał | `seedance_2_5` | 1080p (1664×1248), 6 s, `draft_job_id` z #4, bez audio | 96 | **użyty** → `src/assets/cinematic/sign-in-entrance.mp4` (1440 px, CRF 24, 1,1 MB) |
| 6 | Rejestracja: klatka startowa (niebieska godzina) | `gpt_image_2_5` | high, 2k, 4:3, zmiana światła na `Blobs/StonewashedLinenThrow-1.webp` | 2,75 | **użyty** (`media-work/dawn-final.png`) |
| 7 | Rejestracja: wideo świtu, szkic | `seedance_2_5` | draft, 5 s, 4:3, start #6, koniec `StonewashedLinenThrow-1.webp`, kamera nieruchoma | 15 | zaakceptowany: zmienia się tylko światło |
| 8 | Rejestracja: wideo świtu, finał | `seedance_2_5` | 1080p, 5 s, `draft_job_id` z #7, bez audio | 80 | **użyty** → `src/assets/cinematic/register-dawn.mp4` (1440 px, CRF 24, 0,76 MB) |

Po #8: saldo **858,25** (wydane 229,75).
