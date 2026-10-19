# Microsoft Store listing: draft texts

*Last reviewed: 2026-10-19*

Part of REL-01 phase 4 ([`store-packaging.md`](store-packaging.md)). Wording follows section 6 there: the title is "Deskair" only; compatibility and "unofficial" go in the first lines. Ukrainian is a draft that needs native review (I18N-02). Copy each block into Partner Center.

## Fields

| Field | Value |
|---|---|
| Product name | Deskair |
| Category | Utilities and tools (owner to confirm) |
| Price | Free |
| Search terms | `CO2 monitor`, `air quality`, `CO2 tray`, `carbon dioxide`, `indoor air`, `CO2 history`. `Aranet4` is left out on purpose (policy 10.1.3); it is in the description. |
| Support contact | https://github.com/romanshevchuk/deskair-aranet4/issues |
| Privacy policy URL | https://github.com/romanshevchuk/deskair-aranet4/blob/main/PRIVACY.md (after the file is pushed) |
| Website | https://github.com/romanshevchuk/deskair-aranet4 |
| Hardware requirements | Aranet4 sensor and a PC with Bluetooth LE. |
| Age rating | All answers "no" |

## English

**Short description (up to 1000 characters)**

Unofficial, free desktop monitor for the Aranet4 CO₂ sensor. See live CO₂, temperature, humidity and pressure, get a gentle alert when the air needs fresh air, and look back over days, weeks and months. Everything stays on your PC.

**Description**

Unofficial, free desktop monitor for the Aranet4 CO₂ sensor. Requires an Aranet4 with Bluetooth.

Deskair is an independent open-source project. It is not made by, affiliated with or endorsed by the maker of Aranet4. Aranet is a trademark of its owner.

**Live**
- CO₂ in large numbers, with a clear zone: Good, Elevated or High.
- Plain advice, for example "Open a window for about 10 minutes".
- Temperature, humidity and pressure with small trend charts.

**In the tray**
- An icon that changes colour with the air quality, and a quick flyout with the last 24 hours.
- A gentle notification when the air stays high, with Snooze. Respects quiet hours and Windows Do Not Disturb.

**History**
- Day, week, month and year views, with night-only averages and the hours when the air is usually worst.
- Gaps stay gaps: Deskair never invents data, and says how complete a period is.

**Private**
- No account, no cloud, no tracking. Readings are saved as CSV files in your Documents folder, and you can export them.

**Languages:** English and Ukrainian.

**What you need:** an Aranet4 sensor and a Windows PC with Bluetooth LE. Pair the sensor with Windows once, then start Deskair.

**What's new (first release)**

First release.

**Screenshot captions**

1. Your Aranet4 on your PC: live CO₂ with clear advice
2. The tray flyout: the last 24 hours at a glance
3. CO₂ history by hour, with the worst hours marked
4. Night summary: how fresh the air was while you slept
5. Temperature and humidity in their comfort range

**Notes for certification** (the "Notes to testers" field)

Deskair is a classic .NET desktop app, so it declares `runFullTrust`; it cannot run without it. It declares the `bluetooth` device capability to read an Aranet4 CO₂ sensor over Bluetooth LE. Without the sensor the app shows a welcome screen that explains what it needs. It makes no network connections. Privacy: see the policy link.

## Українська (чернетка, потрібна перевірка носієм)

**Короткий опис**

Неофіційний безкоштовний монітор для датчика CO₂ Aranet4 на комп’ютері. Показує CO₂, температуру, вологість і тиск у реальному часі, делікатно нагадує, коли час провітрити, і зберігає історію за дні, тижні та місяці. Усі дані залишаються на вашому ПК.

**Опис**

Неофіційний безкоштовний монітор для датчика CO₂ Aranet4. Потрібен Aranet4 з Bluetooth.

Deskair — незалежний проєкт з відкритим кодом. Його не створювала компанія-виробник Aranet4, він не пов’язаний з нею і не схвалений нею. Aranet — торгова марка свого власника.

**Наживо**
- CO₂ великими цифрами та зрозуміла зона: Добре, Підвищений або Високий.
- Проста порада, наприклад «Відчиніть вікно приблизно на 10 хвилин».
- Температура, вологість і тиск з маленькими графіками тренду.

**У треї**
- Значок, що змінює колір разом з якістю повітря, і швидке вікно з останніми 24 годинами.
- Делікатне сповіщення, коли повітря довго залишається поганим, з відкладенням. Поважає тихі години та режим «Не турбувати» у Windows.

**Історія**
- Перегляд за день, тиждень, місяць і рік, середні нічні значення та години, коли повітря зазвичай найгірше.
- Прогалини залишаються прогалинами: Deskair нічого не вигадує і показує, наскільки повний період.

**Приватність**
- Без облікового запису, хмари й відстеження. Показники зберігаються як CSV-файли в теці «Документи», їх можна експортувати.

**Мови:** англійська та українська.

**Що потрібно:** датчик Aranet4 і комп’ютер з Windows та Bluetooth LE. Один раз з’єднайте датчик з Windows, потім запустіть Deskair.

**Що нового:** Перший випуск.
