# Treść dialogu potwierdzenia — propozycja do zatwierdzenia

Status: DRAFT — DO WERYFIKACJI PRAWNEJ. Dokument PL: wersja 0.1.0-draft. Teksty poniżej nie zostały wdrożone ani zapisane w bazie.

## Polski

**TITLE:** Regulamin korzystania z GF

**MESSAGE:** Przed rozpoczęciem korzystania z platformy zapoznaj się z regulaminem i potwierdź jego akceptację.

**CHECKBOX:** Zapoznałem(-am) się z Regulaminem korzystania z platformy GF i akceptuję jego postanowienia.

**BUTTON:** Potwierdzam i kontynuuję

## English

**TITLE:** Terms of Use for GF

**MESSAGE:** Before you start using the platform, read the terms of use and confirm your acceptance.

**CHECKBOX:** I have read the Terms of Use of the GF Platform and accept their provisions.

**BUTTON:** Confirm and continue

The Polish document is the canonical legal text. These are proposed interface translations, not an English translation of the document. An English document would require a separately reviewed translation and an expressly defined legal status.

## Granice potwierdzenia

Pole pozostaje początkowo niezaznaczone. Użytkownik potwierdza dokument osobiście; nie podpisuje za Pracodawcę umowy SaaS lub DPA, nie udziela blankietowej zgody RODO, zgody marketingowej ani uprawnienia push, nie zrzeka się praw. Zachować istniejące wyjaśnienie gate o osobistym charakterze potwierdzenia. Dowód przyjęcia nie poświadcza faktycznego przeczytania każdej strony.

## Ograniczenia obecnego interfejsu

- Tytuł i wiadomość pochodzą z jednego rekordu RegulationDocument, bez oddzielnych pól PL/EN. Nie są automatycznie tłumaczone według języka Konta. Obecnie wersje językowe powyżej są materiałem redakcyjnym, nie parametrem przełączanym w panelu.
- Checkbox i przycisk mają istniejące teksty ogólne tłumaczone przez `t(...)`. Panel nie pozwala zastąpić ich powyższą treścią. Wygenerowany PDF nie wymaga takiego zastąpienia; proponowane teksty pozostają do odrębnej decyzji implementacyjnej.
- Publikacja dotyczy jednocześnie pracowników i menedżerów; brak wyboru roli docelowej. Nie należy tworzyć drugiego rekordu językowego jako obejścia: wszyscy otrzymaliby obowiązek potwierdzenia obu PDF.
- Proponowany tytuł mieści się w limicie 160 znaków, wiadomość w 4000, a wersja `0.1.0-draft` w 80. Unikalność wersji należy sprawdzić w rzeczywistej instancji; dokumentu DRAFT nie publikować.

Źródła ograniczeń i instrukcja późniejszej publikacji: [README](README.md).
