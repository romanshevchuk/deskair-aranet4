# Localization

*Last reviewed: 2026-10*

## Languages

- **English (en)**: default and fallback.
- **Ukrainian (uk)**: second language.

On first launch the app follows the Windows display language: Ukrainian if it is Ukrainian, otherwise English. Settings → Language offers System, English and Українська. A change applies immediately, without a restart.

## Technical rules

1. Strings live in `Strings.resx` (English, neutral) and `Strings.uk.resx`. Key format: `Area_Element_Purpose`, for example `Live_Hero_TrendFalling`.
2. Never build sentences by joining pieces. Use whole strings with named placeholders (`{count}`, `{room}`, `{time}`).
3. **Plurals:** English has two forms (one, other). Ukrainian has three for whole numbers.
   - one: n mod 10 = 1 and n mod 100 ≠ 11 (1 день, 21 день)
   - few: n mod 10 in 2..4 and n mod 100 not in 12..14 (2 дні, 23 дні)
   - many: everything else (5 днів, 11 днів)
   Use a small plural helper and store each form as its own resource key.
4. **Numbers and dates** in the UI follow the selected language: English `1,068 ppm`, `22.3 °C`, `Oct 9`; Ukrainian `1 068 ppm` (non-breaking space), `22,3 °C`, `9 жовт.` Time is 24 h in both.
5. **Files never localize.** CSV, JSON, import and export always use the invariant culture (period decimal, ISO 8601 dates).
6. **Do not translate:** "CO₂", unit symbols (ppm, hPa, °C, %), "Aranet4", "Deskair".
7. **Text length:** Ukrainian is often 15 to 30 % longer. No fixed-width buttons or segments. The History toolbar and the Live chart header must still fit. Test every screen in Ukrainian.
8. Zone names are nouns or adjectives that must agree with their sentences. Write full phrases per case (for example "vs previous week", "vs previous month") and do not assemble them.
9. Toasts, tray tooltips, the Store listing (en-US, uk-UA) and the app manifest are localized too.

## Seed strings (Ukrainian is a draft; a native speaker must review it)

| Key (suggested) | English | Ukrainian |
|---|---|---|
| Nav_Live | Live | Наживо |
| Nav_History | History | Історія |
| Nav_Settings | Settings | Налаштування |
| Metric_Temperature | Temperature | Температура |
| Metric_Humidity | Humidity | Вологість |
| Metric_Pressure | Pressure | Тиск |
| Zone_Good | Good | Добре |
| Zone_Elevated | Elevated | Підвищений |
| Zone_High | High | Високий |
| Trend_Falling | Falling | Знижується |
| Trend_Rising | Rising | Зростає |
| Trend_Steady | Steady | Стабільно |
| Trend_Detail | {ppm} ppm over {min} min | {ppm} ppm за {min} хв |
| Advice_Good_Title | Air is fresh, nothing to do | Повітря свіже, нічого робити не потрібно |
| Advice_Good_Text | Below 1,000 ppm, so focus and sleep quality are not affected. | Нижче 1 000 ppm, тож концентрація та якість сну не страждають. |
| Advice_Elevated_Title | Open a window for about 10 minutes | Відчиніть вікно приблизно на 10 хвилин |
| Advice_Elevated_Text | Above the ideal level. Should drop below 1,000 ppm in about {min} min. | Вище за ідеальний рівень. Має опуститися нижче 1 000 ppm приблизно за {min} хв. |
| Advice_High_Title | Ventilate now: open windows fully | Провітріть зараз: відчиніть вікна навстіж |
| Advice_High_Text | Above 1,400 ppm, concentration and sleep suffer. Needs about {min} min to drop below 1,000 ppm. | Понад 1 400 ppm страждають концентрація і сон. Потрібно приблизно {min} хв, щоб опуститися нижче 1 000 ppm. |
| Stat_GoodShare | Good in last 24 h | Добре за останні 24 год |
| Stat_Peak | Peak at {time} | Пік о {time} |
| Range_Day / Week / Month / Year | Day / Week / Month / Year | День / Тиждень / Місяць / Рік |
| Hours_AllDay / Hours_Night | All day / Night | Увесь день / Ніч |
| Group_ByDay / ByWeek / ByMonth | By day / By week / By month | За днями / За тижнями / За місяцями |
| Chart_NoData | No data | Немає даних |
| Chart_TypicalDay | Typical day | Типова доба |
| Stats_Average | Average | Середнє |
| Stats_Highest | Highest reading | Найвище значення |
| Stats_Lowest | Lowest reading | Найнижче значення |
| Stats_ZoneShare | Time in each zone | Час у кожній зоні |
| Gap_DaysNoData (plural) | {count} day with no data / {count} days with no data | one: {count} день без даних; few: {count} дні без даних; many: {count} днів без даних |
| Banner_Stale | Last reading {min} min ago | Останнє значення {min} хв тому |
| Offline_Title | Can't reach your Aranet4 | Не вдається зв'язатися з Aranet4 |
| Offline_Bluetooth | Turn on Bluetooth | Увімкнути Bluetooth |
| Action_Retry | Try again | Спробувати ще раз |
| Footer_Connected | Connected · updated {sec} s ago | Підключено · оновлено {sec} с тому |
| Settings_Language | Language | Мова |
| Settings_Theme | Theme | Тема |
| Theme_Light / Dark / System | Light / Dark / System | Світла / Темна / Системна |
| Settings_Alerts | Alerts | Сповіщення |
| Settings_QuietHours | Quiet hours | Тихі години |
| Settings_Devices | Devices | Пристрої |
| Settings_Data | Data and storage | Дані та сховище |
| Settings_Startup | Start-up | Запуск |
| Toast_Elevated_Title | CO₂ is elevated · {room} | Рівень CO₂ підвищений · {room} |
| Toast_Snooze | Snooze 1 h | Відкласти на 1 год |
| Toast_Dismiss | Dismiss | Закрити |
| Action_OpenApp | Open Deskair | Відкрити Deskair |
