# Localization

*Last reviewed: 2026-10-19*

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
4. **Numbers and dates** in the UI follow the selected language for the decimal mark and dates, but metric values never have a group separator (D-0020): English `1068 ppm`, `22.3 °C`, `1013.2 hPa`, `59%`, `Oct 9`; Ukrainian `1068 ppm`, `22,3 °C`, `1013,2 hPa`, `59 %` (narrow no-break space), `9 жовт.` Time is 24 h in both. Full table: `docs/product/behaviour-rules.md` "Number and time format".
5. **Files never localize.** CSV, JSON, import and export always use the invariant culture (period decimal, ISO 8601 dates).
6. **Do not translate:** "CO₂", unit symbols (ppm, hPa, °C, %), "Aranet4", "Deskair".
7. **Text length:** Ukrainian is often 15 to 30 % longer. No fixed-width buttons or segments. The History toolbar and the Live chart header must still fit. Test every screen in Ukrainian.
8. Zone names are nouns or adjectives that must agree with their sentences. Write full phrases per case (for example "vs previous week", "vs previous month") and do not assemble them.
9. Toasts, tray tooltips, the Store listing (en-US, uk-UA) and the app manifest are localized too.

## Seed strings (Ukrainian is a draft; a native speaker must review it)

| Key (suggested) | English | Ukrainian |
|---|---|---|
| Nav_Live | Live | Зараз |
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
| Advice_Good_Title_Rising | Good now, but rising | Зараз добре, але рівень зростає |
| Advice_Good_Text_Rising | Still below 1,000 ppm. Open a window soon to keep it that way. | Ще нижче 1 000 ppm. Відчиніть вікно найближчим часом, щоб так і лишилося. |
| Advice_Elevated_Title | Open a window for about 10 minutes | Відчиніть вікно хвилин на 10 |
| Advice_Elevated_Text_Falling | Above the ideal level, but dropping. | Вище за ідеальний рівень, але знижується. |
| Advice_Elevated_Text_Steady | Above the ideal level. Ventilating will help. | Вище за ідеальний рівень. Провітрювання допоможе. |
| Advice_Elevated_Text_Rising | Above the ideal level and still rising. Ventilate soon. | Вище за ідеальний рівень і ще зростає. Провітріть незабаром. |
| Advice_High_Title | Ventilate now: open windows fully | Провітріть зараз: відчиніть вікна навстіж |
| Advice_High_Text_Falling | Above 1,400 ppm, concentration and sleep suffer. It is dropping now. | Понад 1 400 ppm страждають концентрація і сон. Зараз знижується. |
| Advice_High_Text_Steady | Above 1,400 ppm, concentration and sleep suffer. | Понад 1 400 ppm страждають концентрація і сон. |
| Advice_High_Text_Rising | Above 1,400 ppm and still rising. Concentration and sleep suffer. | Понад 1 400 ppm і ще зростає. Страждають концентрація і сон. |
| Mini_Temperature_Low | Too cool · ideal {min}–{max} {unit} | Прохолодно · бажано {min}–{max} {unit} |
| Mini_Temperature_High | Too warm · ideal {min}–{max} {unit} | Занадто тепло · бажано {min}–{max} {unit} |
| Mini_Humidity_Low | Too dry · ideal {min}–{max} {unit} | Занадто сухо · бажано {min}–{max} {unit} |
| Mini_Humidity_ABitLow | A bit dry · ideal {min}–{max} {unit} | Трохи сухо · бажано {min}–{max} {unit} |
| Mini_Humidity_ABitHigh | A bit humid · ideal {min}–{max} {unit} | Трохи волого · бажано {min}–{max} {unit} |
| Mini_Humidity_High | Too humid · ideal {min}–{max} {unit} | Занадто волого · бажано {min}–{max} {unit} |
| Stat_GoodShare | Good in last 24 h | Добре за останні 24 год |
| Stat_Peak | Peak at {time} | Пік о {time} |
| Stat_PeakNow | Peak right now | Пік зараз |
| Stat_InZone_Good | Good so far | У цій зоні |
| Stat_InZone_Elevated | Elevated so far | У цій зоні |
| Stat_InZone_High | High so far | У цій зоні |

Ukrainian uses one phrase ("У цій зоні", "in this zone") for all three `Stat_InZone_*` keys because a natural "so far" form with the zone name is too long; the zone is already shown by the hero colour and pill.
| Duration_Minutes | {min} min | {min} хв |
| Duration_LessThanMinute | < 1 min | < 1 хв |
| Duration_HoursMinutes | {h} h {min} min | {h} год {min} хв |
| Chart_Now | Now | Зараз |
| Chart_Ideal | ideal {min}–{max} | бажано {min}–{max} |
| Chart_Summary_OneHour | Last hour · Low {low} · Avg {avg} · High {high} {unit} | Остання година · Мін. {low} · Сер. {avg} · Макс. {high} {unit} |
| Chart_Summary_SixHours | Last 6 hours · Low {low} · Avg {avg} · High {high} {unit} | Останні 6 годин · Мін. {low} · Сер. {avg} · Макс. {high} {unit} |
| Chart_Summary_TwentyFourHours | Last 24 hours · Low {low} · Avg {avg} · High {high} {unit} | Останні 24 години · Мін. {low} · Сер. {avg} · Макс. {high} {unit} |
| Range_1h / Range_6h / Range_24h | 1h / 6h / 24h | 1 год / 6 год / 24 год |
| Footer_ConnectedMinutes | Connected · updated {min} min ago | Підключено · оновлено {min} хв тому |
| Footer_NotConnected | Not connected | Не підключено |
| Footer_Connecting | Connecting to your sensor… | Підключення до датчика… |
| Footer_ConnectingLast | Connecting to your sensor… Last reading {ago} ago | Підключення до датчика… Останнє значення {ago} тому |
| Flyout_Connecting_Title / Flyout_Connecting_Text | Connecting to your Aranet4… / This usually takes a few seconds. | Підключення до Aranet4… / Зазвичай це займає кілька секунд. |
| Footer_Signal_Good / Fair / Weak | Signal good / fair / weak | Сигнал хороший / середній / слабкий |
| Banner_LowBattery | Sensor battery is low ({percent}) | Низький заряд датчика ({percent}) |
| Banner_HistoryUnreadable | Can't read your saved readings. Live data still works. | Не вдається прочитати збережені виміри. Поточні дані працюють. |
| Banner_HistoryOpenFolder | Open data folder | Відкрити папку з даними |
| Action_Dismiss | Dismiss | Закрити |
| Startup_Failed | Deskair could not start: {message} | Не вдалося запустити Deskair: {message} |
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
| Settings_About | About | Про застосунок |
| Settings_About_Unofficial | Unofficial, independent project | Неофіційний незалежний проєкт |
| Settings_About_Text | Deskair is open source and is not made by, affiliated with or endorsed by the maker of Aranet4. github.com/romanshevchuk/deskair-aranet4 | Deskair — застосунок з відкритим кодом. Його не створювала компанія-виробник Aranet4, він не пов’язаний з нею і не схвалений нею. github.com/romanshevchuk/deskair-aranet4 |
| Toast_Elevated_Title | CO₂ is elevated · {room} | Рівень CO₂ підвищений · {room} |
| Toast_Snooze | Snooze 1 h | Відкласти на 1 год |
| Toast_Dismiss | Dismiss | Закрити |
| Action_OpenApp | Open Deskair | Відкрити Deskair |
| Onboarding_Welcome_Title | Welcome to Deskair | Ласкаво просимо до Deskair |
| Onboarding_Welcome_Text | Watch the CO₂, temperature, humidity and pressure from your Aranet4 sensor, and get a nudge when it’s time to open a window. | Стежте за CO₂, температурою, вологістю й тиском з вашого датчика Aranet4 та отримуйте нагадування, коли час відчинити вікно. |
| Onboarding_Step_Pick | Pick your sensor, then name the room | Оберіть датчик і назвіть кімнату |
| Onboarding_Nearby_Title | Sensors found nearby | Датчики поблизу |
| Onboarding_Pair | Pair | Підключити |
| Onboarding_Searching | Looking for sensors… | Пошук датчиків… |
| Onboarding_NoSensor | No sensor found yet. Wake your sensor and keep it close to your PC. | Датчик ще не знайдено. Розбудіть його й тримайте поруч із ПК. |
| Onboarding_BluetoothOff | Bluetooth is off. Turn it on in Windows settings, then try again. | Bluetooth вимкнено. Увімкніть його в налаштуваннях Windows і спробуйте ще раз. |
| Onboarding_Room_Title / Name | Room / Name | Кімната / Назва |
| Onboarding_Room_NameHint | Shown in the title bar and notifications | Показується в заголовку вікна та у сповіщеннях |
| Onboarding_Room_DefaultName | My room | Моя кімната |
| Onboarding_PairFailed | Could not add the sensor. Try again. | Не вдалося додати датчик. Спробуйте ще раз. |
| Settings_Appearance / Settings_Unit | Appearance and units / Temperature unit | Вигляд і одиниці / Одиниця температури |
| Settings_Alerts_Enabled / Elevated / High | Alerts / Notify at Elevated / Notify at High | Сповіщення / Сповіщати при підвищеному / Сповіщати при високому |
| Settings_Startup_Windows | Start with Windows | Запускати з Windows |
| Settings_Devices_RoomName / None / Add | Room name / No sensors yet / Add a sensor | Назва кімнати / Датчиків ще немає / Додати датчик |
| Action_Add / Action_Cancel | Add / Cancel | Додати / Скасувати |
| Footer_Syncing | · Syncing history… {percent} | · Синхронізація історії… {percent} |
| Action_Import / Action_Export | Import / Export | Імпортувати / Експортувати |
| Settings_Data_Folder / OpenFolder | Data folder / Open folder | Папка даних / Відкрити папку |
| Settings_Data_Import / ImportHint / ChooseFile | Import from the phone app / Fills gaps with the readings in the phone app’s export / Choose file | Імпорт із застосунку на телефоні / Заповнює прогалини вимірюваннями з експорту застосунку на телефоні / Вибрати файл |
| Settings_Data_Export / ExportHint | Export to CSV / All stored readings of the selected room | Експорт у CSV / Усі збережені вимірювання вибраної кімнати |
| Import_Preview (plural) | {count} reading(s), {first} to {last} · {new} new · {present} already present | {count} вимірювання/вимірювань, {first} – {last} · нових: {new} · уже є: {present} |
| Import_Done (plural) | {count} new reading(s) added. | Додано {count} нове/нові/нових вимірювання/вимірювань. |
| Import_Empty / Failed / Unavailable / DialogTitle | No usable readings / Could not read this file / Importing is not available for this sensor / Choose the phone app’s export | Немає придатних вимірювань / Не вдалося прочитати файл / Для цього датчика імпорт недоступний / Виберіть експорт із застосунку на телефоні |
| Export_Done / Failed / DialogTitle / FileType | Saved {file}. / Could not save the file. / Export readings / CSV file | Збережено {file}. / Не вдалося зберегти файл. / Експорт вимірювань / Файл CSV |
| Data_NoSensor / Dialog_AllFiles | Add a sensor first. / All files | Спочатку додайте датчик. / Усі файли |
| History_Empty_Title / Text / Import | Collecting your first data / (explanation) / Import from the phone app | Збираємо перші дані / (пояснення) / Імпорт із застосунку на телефоні |
| Settings_Data_ChangeFolder / ChangeTitle | Change / Choose the data folder | Змінити / Виберіть теку даних |
| Data_Folder_Restart / Failed | Deskair will use {folder} after you restart it. Your existing files are not moved: copy them there yourself to keep your history. / Could not use this folder. Choose another one. | Deskair використовуватиме {folder} після перезапуску. Наявні файли не переносяться: скопіюйте їх туди самостійно, щоб зберегти історію. / Не вдалося використати цю теку. Виберіть іншу. |
| Stats_Change_Day / Week / Month / Year / None | vs previous 24 hours / vs previous week / vs previous 30 days / vs previous 12 months / no earlier data to compare | порівняно з попередніми 24 годинами / з попереднім тижнем / з попередніми 30 днями / з попередніми 12 місяцями / немає раніших даних для порівняння |
| Stats_BestPeriod / WorstPeriod / LowestPeriod / HighestPeriod | Best {period} / Worst {period} / Lowest {period} / Highest {period} | Найкращий / Найгірший / Найнижчий / Найвищий {period} (день, тиждень and місяць are all masculine) |
| Stats_Period_Day / Week / Month | day / week / month | день / тиждень / місяць |
| Insight_WorstHours / AllGood | **Worst hours: {from}–{to}** (avg {value}). Opening a window shortly before keeps you in the good zone. / **All good.** On a typical day CO₂ stays in the good zone, even at its worst around {from}–{to} (avg {value}). | Найгірші години: {from}–{to} (середнє {value}). Відчинене вікно незадовго до цього тримає повітря в зеленій зоні. / Зазвичай CO₂ залишається в зеленій зоні, навіть у найгірші години, {from}–{to} (середнє {value}). | (the `**` marks a bold lead, see `InlineMarkup`)
| Offline_Text / Text_BluetoothOff | The sensor may be out of range or asleep. / Bluetooth is off, so Deskair cannot reach the sensor. | Датчик може бути поза зоною дії або «спати». / Bluetooth вимкнено, тож Deskair не може зв'язатися з датчиком. |
| Offline_LastReading / HistoryHint | Your last reading was {value} at {time}. / History is still available. | Останнє вимірювання: {value}, {time}. / Історія, як і раніше, доступна. |
| Offline_Step_Range / Wake / Battery | Stay within about 10 m of the sensor / Press the button on the Aranet4 to wake it / Check the sensor battery ({percent} when last seen) | Тримайтеся приблизно в межах 10 м від датчика / Натисніть кнопку на Aranet4, щоб розбудити його / Перевірте батарею датчика ({percent} востаннє) |
| Tray_AppName / Tray_Quit | Deskair / Quit | Deskair / Вийти |
| Flyout_Updated / Flyout_UpdatedMinutes | updated {sec} s ago / updated {min} min ago | оновлено {sec} с тому / оновлено {min} хв тому |
| Duration_Hours | {h} h | {h} год |
| Toast_High_Title | Ventilate now · {room} | Провітріть зараз · {room} |
| Toast_Elevated_Body / Toast_High_Body | {value}. Open a window for about 10 minutes. / {value}. Open windows fully. | {value}. Відчиніть вікно приблизно на 10 хвилин. / {value}. Відчиніть вікна повністю. |
| Settings_TrayValue / Settings_TrayValue_Hint | Show CO₂ on tray icon / The icon shows the value (1.1k) instead of a ring | Показувати CO₂ на значку в треї / Значок показує значення (1.1k) замість кільця |
| Tray_SnoozeTomorrow / Tray_ResumeUntil | Snooze until tomorrow / Resume alerts (snoozed until {time}) | Відкласти до завтра / Відновити сповіщення (відкладено до {time}) |
| Flyout_Snooze / Flyout_SnoozedUntil / Flyout_Resume | Snooze / Snoozed until {time} / Resume alerts | Відкласти / Відкладено до {time} / Відновити сповіщення |
| Action_Retrying / Action_RetryFailed | Trying… / Still can't reach it. Try again | Пробуємо… / Досі не вдається підключитися. Спробувати ще раз |
| Hours_NightHint | The night hours from Settings | Години ночі з налаштувань |
| History_Chart_Hourly / Daily / Weekly / Monthly | readings of the last 24 hours / daily range & average / weekly range & average / monthly range & average | виміри за останні 24 години / діапазон і середнє за день / тиждень / місяць |
| History_Chart_NightsOnly | {what} · nights only | {what} · лише ночі |
| History_Legend_Bars / Line / NoData | Bar = min–max · tick = average / Line = readings / No data | Стовпчик = мін–макс · риска = середнє / Лінія = виміри / Немає даних |
| History_TypicalDay_Subtitle, Last7 / Last30 / Last90 | average by hour, {period} / last 7 days / last 30 days / last 90 days | середнє за годину, {period} / останні 7 / 30 / 90 днів |
| History_TypicalDay_Worst | worst {from}–{to}h | найгірше {from}–{to} год |
| Chart_Today / Chart_Avg | Today / avg | Сьогодні / сер. |
| Stats_Header_Average / Stats_Header_NightAverage | Average · {range} / Night average · {range} | Середнє · {range} / Середнє за ніч · {range} |
| Settings_Sleep / Settings_Sleep_Start / Settings_Sleep_End | Sleep / Night starts / Night ends | Сон / Ніч починається / Ніч закінчується |
| Settings_Sleep_Hint | The night band and the Nights and Last night sections in History use these hours | Ці години визначають смугу ночі та розділи «Ночі» й «Остання ніч» в Історії |
| Settings_Sleep_SameTime | The night must start and end at different times. | Ніч має починатися й закінчуватися в різний час. |
| Sleep_Title | Sleep · {from} – {to} | Сон · {from} – {to} |
| Sleep_Verdict_Good / Fair / Poor | Good night / Fair night / Poor night | Гарна ніч / Посередня ніч / Погана ніч |
| Sleep_TimeAbove / Sleep_HoursOf | Time above {limit} {unit} / {hours} h of {total} h | Час вище {limit} {unit} / {hours} год із {total} год |
| Sleep_Peak / Sleep_Deep | Peak at {time} / Deep sleep avg (02–05 h) | Пік о {time} / Середнє в глибокому сні (02–05) |
| Sleep_Tip_Ajar | Leaving the bedroom door ajar or a window tilted usually shortens the time above the limit. | Прочинені двері спальні або відхилене вікно зазвичай скорочують час вище межі. |
| Sleep_Tip_Fresh / Sleep_Tip_Other | Air stayed fresh through the night. / Overnight values for the night hours. | Повітря залишалося свіжим усю ніч. / Нічні значення за години ночі. |
| Sleep_Partial | Only {recorded} of {total} hours were recorded: Deskair was not connected for the rest. | Записано лише {recorded} із {total} год: Deskair не був підключений решту часу. |
| Sleep_Empty | No readings from last night. | Немає даних за минулу ніч. |
| Tray_AlertsPausedByWindows | Alerts paused by Windows | Сповіщення призупинено Windows |
| Settings_Alerts_PausedByWindows | Alerts paused by Windows: Do Not Disturb is on, so Windows holds the notifications. | Сповіщення призупинено Windows: увімкнено «Не турбувати», тому Windows утримує сповіщення. |
| -0/ To / Invalid | From / Until (names of the two time fields) / Use hours and minutes, like 22:00. | З / До / Вкажіть години й хвилини, наприклад 22:00. |
| Settings_QuietHours_Between / Separator | Active between / to | Діють з / до |
