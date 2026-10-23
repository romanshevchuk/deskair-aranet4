# Package size and startup: the plan

*Last reviewed: 2026-10-23*

Backlog items: SIZE-02, PERF-01 to PERF-06 (`backlog.md`, R3). Written for the agent who builds them (one item per branch, in the order below) and for the owner, who does the checks marked **Owner**. Source: a review of the solution on 2026-10-22. Every number here was measured on the owner's PC on that day.

Read first: `AGENTS.md`, `docs/engineering/architecture.md`, `packaging/README.md`.

## 0. Already done (TIDY-01, branch `fix/TIDY-01-dead-code`, 2026-10-22)

| Change | Effect |
|---|---|
| Removed `HourAxisLayer`, `HourBar`, `TimeSlot` (and its tests), the `ZoneChanged` message (never sent; its `LiveViewModel` handler and test), `ReadingQueryService.HasReadingsAsync` (used only by tests) | Less code. Build, 1662 tests and the format check pass |
| `<SatelliteResourceLanguages>en;uk</SatelliteResourceLanguages>` in `Deskair.App.csproj` | The 13 framework languages the app does not ship are gone: 167 → 159 MB installed, 443 → 287 files, `.msix` 69.2 → 65.8 MB |
| Fixed the comment on `PackagedStartupRegistration` (it does block the caller) | Comment only |

Kept on purpose, do not remove: `LocalCalendar.DayLength` and `NumberFormat.Whole`/`OneDecimal` (their tests guard daylight-saving days and the Ukrainian decimal comma), `ToastIdentity.Unregister` (a future winget uninstaller needs it), and `ChartArea.DataY` (the pair of `DataX`).

**Owner, before merging TIDY-01:** run `dotnet run --project src/Deskair.App -- --simulated`, switch the language to Ukrainian in Settings, and check that the text is Ukrainian and numbers use a decimal comma. The SIZE-02 package check below covers the packaged build too.

## 1. Order and size

| ID | What | Gain (measured or estimated) | Risk | Owner yes needed first? |
|---|---|---|---|---|
| SIZE-02 | Trim only the Windows SDK projection on publish | `.msix` 65.8 → 60.0 MB; installed 159 → 136 MB (measured) | Medium: needs the full packaged check | No (no package or project change), but the owner must run section 3 |
| PERF-01 | Load the reading files off the UI thread | The UI no longer freezes while readings load: about 250k rows a year, growing forever | Low | No |
| PERF-02 | Replace the generic host with a plain `ServiceCollection` | About 25 fewer assemblies loaded at start; no config file watcher; no Windows Event Log writes | Low to medium | **Yes**: it changes package references (`AGENTS.md`) |
| PERF-03 | Stop the footer's 1 s tick while the main window is hidden | No 3600 wakeups an hour while in the tray | Low | No |
| PERF-04 | Drop the extra sorts of reading lists the store already returns sorted | Less work on History Year loads | Low | No |
| PERF-05 | Read the startup task state asynchronously | No UI block in Settings load (packaged build) | Low | No |
| PERF-06 | Measure ReadyToRun for the app's own assemblies | Unknown; decide on numbers | None (measure only) | Owner decides from the numbers |

Each item: its own branch (`feature/<ID>-short-name` from `main`), its own commit(s) starting with the ID, and the usual checks before you hand it over:

```powershell
dotnet build Deskair.sln
dotnet test tests/Deskair.Tests
dotnet format Deskair.sln --verify-no-changes
```

Do not commit until the owner asks (owner preference). Stop and ask when anything below does not match what you find in the code.

## 2. SIZE-02: trim only the Windows SDK projection

**Why.** `Microsoft.Windows.SDK.NET.dll` is 23.7 MB, the largest file in the package. The app uses only a few WinRT namespaces (`Windows.Devices.Bluetooth*`, `Windows.Devices.Radios`, `Windows.Devices.Enumeration`, `Windows.Storage.Streams`, `Windows.UI.Notifications`, `Windows.Data.Xml.Dom`, `Windows.ApplicationModel`). WPF does not support trimming, so only this one assembly is trimmed and every other assembly is copied unchanged. That keeps WPF's reflection safe. Measured: the DLL goes from 23.7 MB to 0.52 MB; `PresentationFramework.dll` and `System.Private.CoreLib.dll` stay byte for byte the same size.

**Change** (only `src/Deskair.App/Deskair.App.csproj`). Add:

```xml
  <!-- Store package size (docs/plans/size-and-startup.md, SIZE-02): publish trims only the Windows SDK projection;
       WPF does not support trimming, so every other assembly is copied unchanged. -->
  <PropertyGroup>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>partial</TrimMode>
    <_SuppressWpfTrimError>true</_SuppressWpfTrimError>
    <SuppressTrimAnalysisWarnings>true</SuppressTrimAnalysisWarnings>
  </PropertyGroup>

  <!-- PublishTrimmed also switches off runtime features for the whole app (built-in COM, type descriptors,
       System.Text.Json reflection, ...). WPF and our JSON settings need them, so keep the untrimmed defaults. -->
  <PropertyGroup>
    <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
    <_ComObjectDescriptorSupport>true</_ComObjectDescriptorSupport>
    <_DefaultValueAttributeSupport>true</_DefaultValueAttributeSupport>
    <CustomResourceTypesSupport>true</CustomResourceTypesSupport>
    <EnableUnsafeBinaryFormatterSerialization>true</EnableUnsafeBinaryFormatterSerialization>
    <EnableUnsafeUTF7Encoding>true</EnableUnsafeUTF7Encoding>
    <MetadataUpdaterSupport>true</MetadataUpdaterSupport>
    <StartupHookSupport>true</StartupHookSupport>
    <JsonSerializerIsReflectionEnabledByDefault>true</JsonSerializerIsReflectionEnabledByDefault>
    <EnableConsumingManagedCodeFromNativeHosting>true</EnableConsumingManagedCodeFromNativeHosting>
    <EnableCppCLIHostActivation>true</EnableCppCLIHostActivation>
  </PropertyGroup>

  <Target Name="TrimOnlyWindowsSdk" AfterTargets="PrepareForILLink">
    <ItemGroup>
      <ManagedAssemblyToLink Condition="'%(Filename)' != 'Microsoft.Windows.SDK.NET'">
        <IsTrimmable>false</IsTrimmable>
        <TrimMode>copy</TrimMode>
      </ManagedAssemblyToLink>
    </ItemGroup>
  </Target>
```

A first version without the second property group published fine but crashed at start (2026-10-23): `PublishTrimmed` also turns off runtime features such as built-in COM for the whole app, and WPF needs them. A successful publish proves nothing; always start the published app.

`PublishTrimmed` only acts on `dotnet publish`, so `dotnet build`, `dotnet run` and the tests behave as before. It was tried on 2026-10-22 with `TreatWarningsAsErrors` on: it publishes without errors.

**Agent checks.**

1. The three commands in section 1.
2. `.\packaging\build-msix.ps1 -Version 0.1.90.0` succeeds. Record the size it prints (expected about 60 MB).
3. In `artifacts\msix\publish`: `Microsoft.Windows.SDK.NET.dll` is under 1 MB, `PresentationFramework.dll` is still about 15 MB, and the only culture folder is `uk`.
4. Docs: in `packaging/README.md`, change "about 66 MB" to the new size and add one line on the trimming. In `docs/decisions.md`, add a decision entry with the next free number (check the highest number; it is D-0029): "Publish trims only the Windows SDK projection", with the reason, the internal switch `_SuppressWpfTrimError`, and the fallback (delete the block).

**Status 2026-10-23: block added, agent checks 1 to 4 pass (`.msix` 60 MB, SDK DLL 0.52 MB, `PresentationFramework.dll` 15.08 MB, only `uk` culture folder, 135.9 MB installed, 1662 tests pass); docs done (D-0029). The trimmed build starts and shows its window with `--simulated` (after the fix for a start-up crash); everything that talks to Windows is still unchecked. Waiting for the owner.**

**Owner check: see section 3** (both parts). The item is done only when section 3 passes. If anything fails, delete the block, rebuild and say what failed. There is no partial fix to try.

## 3. Owner: testing the trimmed build (SIZE-02, also covers TIDY-01)

Trimming can only break code that calls WinRT, so the check is everything that talks to Windows: Bluetooth, toasts, the startup task. Allow about 45 minutes.

### 3a. Unpackaged trimmed build (cheap, do first)

Quit the Store Deskair first (tray icon, Quit), because only one Deskair can run at a time.

```powershell
dotnet publish src\Deskair.App\Deskair.App.csproj -c Release -r win-x64 --self-contained true -o artifacts\trimmed
.\artifacts\trimmed\Deskair.App.exe --simulated --test-toast
```

| # | Check | Pass when |
|---|---|---|
| 1 | Window opens; Live, History and Settings show | No error box, values move |
| 2 | The test toast appears (the toast has two buttons, Snooze and Dismiss; pressing the body is "Open"). Press **Snooze**; then start again and press the **toast body** | Snooze shows in the tray menu; the body press brings the window up |
| 3 | Quit, then run `.\artifacts\trimmed\Deskair.App.exe --sensor C3DEC8FCC7C6` (your sensor, its own temp data folder) | Footer goes Connecting, then a history sync, then Connected; live values update within 2 to 4 minutes |
| 4 | Turn Bluetooth off in Windows quick settings, then on again | The app shows the Bluetooth-off state, then recovers |
| 5 | Logs: `%LOCALAPPDATA%\Deskair\logs\deskair-<today>.log` | No `TypeLoadException`, `MissingMethodException` or `InvalidCastException` |

Note: the unpackaged run writes the toast registry entry with the `artifacts\trimmed` path. That does no harm: the Store app ignores it, and the next `dotnet run` writes its own path again.

### 3b. Packaged trimmed build (the real thing)

The Store version and a test package share one identity, so they cannot be installed side by side.

1. **Note your data folder** (Settings → Data and storage). Readings in `Documents\Deskair` survive an uninstall; a custom data-folder choice, the logs and Start with Windows do not.
2. Uninstall the Store Deskair: `Get-AppxPackage RomanShevchuk.Deskair | Remove-AppxPackage`.
3. Build, sign and install (same as phase 3 in `packaging/README.md`; the certificate may still be trusted from 2026-10-18):

   ```powershell
   .\packaging\build-msix.ps1 -Version 0.1.90.0
   .\packaging\sign-test.ps1 -Package artifacts\msix\Deskair_0.1.90.0_x64.msix
   # only if the certificate is no longer trusted, in an administrator window:
   .\packaging\trust-test-cert.ps1
   Add-AppxPackage artifacts\msix\Deskair_0.1.90.0_x64.msix
   ```

4. Start Deskair from the Start menu and check:

| # | Check | Pass when |
|---|---|---|
| 1 | It opens on your real sensor and data (pick the data folder again if you had a custom one) | History shows your months |
| 2 | Sync history (Settings → Data and storage) | Footer shows the sync; the phone app can connect afterwards |
| 3 | Live values keep coming for 10 minutes | Hero and chart update every 2 minutes |
| 4 | A real toast: breathe on the sensor until CO₂ is above 1400 ppm (5 to 7 minutes), or try `Invoke-CommandInDesktopPackage -PackageFamilyName RomanShevchuk.Deskair_0vc1xnb0gk962 -AppId App -Command ((Get-AppxPackage RomanShevchuk.Deskair).InstallLocation + '\Deskair.App.exe') -Args '--test-toast'` with Deskair closed (it may need Developer Mode) | Toast with the ring picture; Snooze works, and pressing the toast body opens the window; both also work with the app quit |
| 5 | Settings → Start with Windows on, sign out and in | Deskair starts in the tray, window hidden |
| 6 | Language Ukrainian | Text Ukrainian, decimal comma |
| 7 | Size: Windows Settings → Apps → Installed apps → Deskair | About 136 MB (was about 167 MB) |

5. Clean up: `Get-AppxPackage RomanShevchuk.Deskair | Remove-AppxPackage`, reinstall Deskair from the Store, turn Start with Windows on again and pick your data folder again if it was custom. Remove the test certificate if you like (`.\packaging\trust-test-cert.ps1 -Remove` in an administrator window).

## 4. PERF-01: load the reading files off the UI thread

**Why.** `MonthlyCsvStore.LoadAsync` (`src/Deskair.Storage/Readings/MonthlyCsvStore.cs`) reads every month file of a device the first time anything asks for that device. The first ask comes from `AppServicesStarter.StartAsync` on the UI thread, and Storage never leaves the caller's context (no `Task.Run`, no `ConfigureAwait(false)`). So every row is split and parsed on the UI thread right after the window appears. On 2026-10-22 the owner had 57k rows after four months at a 2 minute interval, which is about 250k rows a year, and history is never deleted.

**Change.**

1. In `MonthlyCsvStore.LoadAsync`, keep the cache lookup as it is. Move the disk part into a new private **static** method `ReadAll(string folder, CancellationToken cancellationToken)` that returns `List<Reading>`. It reads synchronously with `File.ReadLines` and calls `cancellationToken.ThrowIfCancellationRequested()` once per file. Call it with `await Task.Run(() => ReadAll(folder, cancellationToken), cancellationToken)`. Keep `index.Add(loaded)` and `indexes[device] = index` after the await, as now. The `gate` semaphore already makes the cache safe.
2. Log once per device load, at Information: `"Loaded {Count} readings for {Address} in {Milliseconds} ms."` with `device.Address` (a `DeviceId` record prints as a long `DeviceId { ... }`). Inject `ILogger<MonthlyCsvStore>` and `TimeProvider` through the constructor (both are registered already; check `AddDeskairStorage` in `src/Deskair.Storage`), and time it with `timeProvider.GetTimestamp()` / `GetElapsedTime`. Use the `if (logger.IsEnabled(...))` guard that the rest of the code uses. Update every `new MonthlyCsvStore(...)` in the tests: pass `NullLogger<MonthlyCsvStore>.Instance` and the fake clock the tests already use.
3. No change to the interface, the file format or the behaviour.

**Tests** (`tests/Deskair.Tests/Storage/MonthlyCsvStoreTests.cs`; all existing tests must pass unchanged):

| Test | Asserts |
|---|---|
| `A_cancelled_first_load_throws_and_a_later_load_reads_the_files` | Write two month files; call `ReadRangeAsync` with a token that is already cancelled → `OperationCanceledException`; call again with a live token → all rows come back (nothing was cached by the cancelled load) |
| `Rows_from_every_month_file_are_loaded_in_time_order` | Only if no existing test covers it: three month files written out of order, one range over all of them, oldest first |

**Owner check (5 minutes).** Make a large test data set for the simulated app (it lives in `%TEMP%\Deskair-simulated`, never your real data):

```powershell
# 1. Run once so the simulated rooms exist, then quit from the tray:
dotnet run --project src/Deskair.App -- --simulated
# 2. Fill room "Office" with three years of readings every 2 minutes (789k rows, about 3 s):
$folder = "$env:TEMP\Deskair-simulated\devices\simulated-SIM-0001"
$inv = [Globalization.CultureInfo]::InvariantCulture
$end = [DateTime]::UtcNow.Date; $t = $end.AddYears(-3)
while ($t -lt $end) {
    $month = $t.ToString('yyyy-MM', $inv); $sb = [Text.StringBuilder]::new("ts_utc,co2_ppm,temp_c,rh_pct,pres_hpa,src`n")
    while ($t -lt $end -and $t.ToString('yyyy-MM', $inv) -eq $month) {
        $co2 = 600 + [int](400 * [Math]::Sin($t.Hour / 24.0 * 2 * [Math]::PI))
        [void]$sb.Append($t.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", $inv)).Append(",$co2,22.5,45,1013.2,dev`n")
        $t = $t.AddMinutes(2)
    }
    [IO.File]::WriteAllText((Join-Path $folder "readings-$month.csv"), $sb.ToString())
}
```

Then compare `main` with the PERF-01 branch, each with `dotnet run -c Release --project src/Deskair.App -- --simulated`:

| Check | `main` (expected) | PERF-01 (pass when) |
|---|---|---|
| Right after the window appears, drag it around and hover the metric tabs | Stutters or freezes for a moment | Moves smoothly; the values appear a little later |
| `%LOCALAPPDATA%\Deskair\logs\deskair-<today>.log` | no load line | A line `Loaded 789120 readings for SIM-0001 in N ms` (more if the simulator added some) (write N into the PR) |
| History → Year | Shows three years | Same |

Afterwards delete `%TEMP%\Deskair-simulated` to go back to a small simulated data set.

## 5. PERF-02: replace the generic host with a plain service collection

**Owner: say yes or no before the agent starts.** It removes the `Microsoft.Extensions.Hosting` package and adds `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging` (same version, 10.0.12) in `Directory.Packages.props` and `Deskair.App.csproj`. `AGENTS.md` requires asking first.

**Why.** `Program.Main` uses `Host.CreateApplicationBuilder(args)` only to get a DI container. The host is never started, and nothing reads `IConfiguration`, `IOptions`, `IHostEnvironment` or `IHostApplicationLifetime` (checked 2026-10-22). It still sets up JSON, environment-variable, user-secrets and command-line configuration (probably with a file watcher for `appsettings.json` in the working directory) and the Console, Debug, EventSource and **EventLog** loggers. The EventLog logger writes the app's warnings to the Windows Application event log as well as to Deskair's own log files. It also ships about 25 extra assemblies.

**Change** (`src/Deskair.App/Program.cs` and the two project files):

```csharp
var services = new ServiceCollection();
services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Information));
services.AddDeskair(options);
services.AddSingleton(instance);

using var provider = services.BuildServiceProvider();

var app = new App(provider);
```

- `RollingFileLoggerProvider` is already registered as an `ILoggerProvider` in `ServiceRegistration.AddWindowsPlatform`, and `AddLogging` picks it up. Keep that registration.
- Before deleting the host, search `src` and `tests` for `IConfiguration`, `IOptions`, `IHostEnvironment`, `IHostApplicationLifetime`, `IHostedService`, `Microsoft.Extensions.Hosting`. If anything turns up, stop and report it.
- `AppServicesStarterTests` and other tests that build the real composition must still pass. If a test used to get logging from the host, give it `services.AddLogging()`.

**Test to add** (`tests/Deskair.Tests/Platform/` next to the composition tests): `The_composition_resolves_the_main_services_without_a_host`. Build `new ServiceCollection().AddLogging().AddDeskair(new AppOptions(Gallery: false, Simulated: true))`, then resolve `StartupSequence`, `AppServicesStarter`, `ShellViewModel` and `ILogger<App>`. Keep it non-UI. If resolving needs WPF objects, resolve only the non-UI ones and say so.

**Agent checks.** The three commands; `artifacts\msix\publish` (from `build-msix.ps1`) no longer contains `Microsoft.Extensions.Hosting.dll`, `Microsoft.Extensions.Configuration*.dll` or `Microsoft.Extensions.Logging.Console.dll`.

**Owner check.** Run `dotnet run --project src/Deskair.App -- --simulated` and quit after a minute: today's log file has the "Deskair started" line. Run once with `--simulated --test-toast`: the toast works. Optional: Event Viewer → Windows Logs → Application has no new ".NET Runtime" entries from Deskair.

## 6. PERF-03: no footer tick while the window is hidden

**Why.** `FooterViewModel.TickAsync` posts a refresh to the UI thread every second for as long as the app runs. The app lives in the tray most of the time, where nobody sees the footer.

**Change.**

1. `FooterViewModel`: add `public void SetWindowShown(bool shown)`. While not shown, `TickAsync` keeps waiting but does not post `Refresh`. When it changes to shown, post one `Refresh` at once so the text is current, and ticking goes on. The default is shown, so nothing changes for tests or the gallery.
2. `MainWindowController` (`src/Deskair.App/Platform/Tray/MainWindowController.cs`) already holds the `ShellViewModel`, and `ShellViewModel.Footer` is the footer, so no new dependency or message is needed. In `Attach`, subscribe to `window.IsVisibleChanged` and `window.StateChanged`. Call `shell.Footer.SetWindowShown(window.IsVisible && window.WindowState != WindowState.Minimized)` from both handlers, and once in `Attach` itself.
3. Do not touch the flyout. It has its own view model and refreshes when it opens.

**Tests** (`tests/Deskair.Tests/ViewModels/FooterViewModelTests.cs`, with the fake clock and dispatcher those tests already use):

| Test | Asserts |
|---|---|
| `A_hidden_window_gets_no_footer_ticks` | `SetWindowShown(false)`, advance 10 s → no `Refresh` posted (count the posted actions as the other tests do) |
| `Showing_the_window_refreshes_the_footer_at_once` | Hidden, advance 90 s, `SetWindowShown(true)` → one post right away, and the text says the right minutes |
| `Ticks_resume_after_the_window_is_shown_again` | Hidden, shown, advance 3 s → three posts |

**Owner check.** Run with `--simulated` and close the window to the tray. After a minute, open it from the tray: the footer shows the right "N min ago" at once, with no wrong text flashing first. Minimize and restore: same.

## 7. PERF-04: no second sort of readings the store already sorted

**Why.** `ReadingIndex` keeps readings sorted, so `MonthlyCsvStore.ReadRangeAsync` returns them oldest first. `ReadingQueryService.GetRangeAsync` and `GetRecentAsync` and `ExportService` still sort again with `OrderBy`, which allocates a full copy each time. History Year works on a year of rows.

**Change.**

1. `IReadingStore.ReadRangeAsync`: add a summary saying the readings are oldest first, `start` inclusive, `end` exclusive.
2. `tests/Deskair.Tests/Fakes/FakeReadingStore.ReadRangeAsync`: return `.OrderBy(reading => reading.Timestamp)`, so the fake keeps the contract. **Do not** change its `<= end` to `< end` in this item, even though the real store is end-exclusive: that would change many tests. Add it to `docs/plans/questions.md` as a question for a separate item instead.
3. Remove the `OrderBy` in `ReadingQueryService.GetRangeAsync`, `ReadingQueryService.GetRecentAsync` and `ExportService` (`src/Deskair.Services/Import/ExportService.cs`). Leave the sorts in Domain (`BucketAggregator`, `SleepCalculator`, `TrendCalculator`): pure functions there cannot assume where their input came from.
4. Leave `HistoryViewModel` line ~391 (`recent.OrderBy` before `AirImprovedDetector.Detect`) unless you have checked that `recent` comes straight from the query service.

**Tests.** All existing tests must pass. Add `Reading_ranges_come_back_oldest_first` to `MonthlyCsvStoreTests` (merge readings out of order across two months, read the range, assert the order), so the contract is tested where it is kept.

**Owner check.** None beyond a look at History Day, Week, Month and Year on real data: same as before.

## 8. PERF-05: read the startup task state without blocking

**Why.** `PackagedStartupRegistration.IsEnabled` waits on a thread-pool task with `.GetAwaiter().GetResult()`. It is called from `StartupSettingsViewModel.LoadAsync` while the app starts, so the UI thread waits on Windows (packaged build only).

**Change.**

1. `IStartupRegistration`: replace `bool IsEnabled { get; }` with `Task<bool> IsEnabledAsync(CancellationToken cancellationToken)`.
2. `PackagedStartupRegistration`: `await StartupTask.GetAsync(TaskId)` directly. Keep the catch for `InvalidOperationException` and `COMException` → `false`. Drop the `Task.Run` and the comment about it.
3. `StartupRegistration` (Run key): return `Task.FromResult(...)` of the current check.
4. `StartupSettingsViewModel.LoadAsync`: `SetWithoutApplying(await registration.IsEnabledAsync(cancellationToken))`. Look for any other caller of `IsEnabled` (search the solution) and update it the same way.
5. Update the fakes in `tests/Deskair.Tests/Fakes` (search `IStartupRegistration`).

**Tests.** The existing `StartupSettingsViewModel` tests pass with the async fake. Add `A_registration_that_cannot_be_read_shows_start_with_windows_off` if no such test exists (the fake throws or returns false).

**Owner check.** Packaged builds only, so it can ride along with the next package check (section 3b row 5): the toggle shows the right state after a restart.

## 9. PERF-06: measure ReadyToRun (measure only, change nothing)

**Why.** The .NET and WPF assemblies in the package are already precompiled. The app's own assemblies (`Deskair.*.dll`, about 1.2 MB) and `CommunityToolkit.Mvvm` are compiled at run time on first use. ReadyToRun could shorten a cold start, at the cost of a larger package.

**Agent.** Publish twice into `artifacts\r2r-off` and `artifacts\r2r-on` (the second with `-p:PublishReadyToRun=true`, otherwise exactly as `build-msix.ps1` publishes). Report the size of each folder and of each `Deskair.*.dll`. Do not change any project file.

**Agent results (2026-10-23).** Published from the SIZE-02 branch (so the Windows SDK projection is trimmed in both); folders `artifacts\r2r-off` and `artifacts\r2r-on` are left on the agent's PC for the owner's runs. No project file was changed.

| | ReadyToRun off | ReadyToRun on |
|---|---|---|
| Whole folder | 135.9 MB (287 files) | 140.0 MB (287 files) |
| `Deskair.App.dll` | 782 KB | 1,608 KB |
| `Deskair.Domain.dll` / `Services` / `Sensors` / `Storage` | 85 / 131 / 59 / 37 KB | 220 / 332 / 144 / 100 KB |
| `CommunityToolkit.Mvvm.dll` | 180 KB | 364 KB |
| Warm time to window (`--simulated`, 3 runs, not a cold start) | 660, 661, 678 ms (middle 661 ms) | 962, 599, 605 ms (middle 605 ms) |

So ReadyToRun costs about 4.1 MB (about 3 %) and saved about 56 ms in a warm start; the first "on" run (962 ms) was a first touch of a new folder. By the rule below (more than about 100 ms for a few MB) this leans to "not worth it", but only the owner's cold-start runs decide.

**Owner.** For each folder: restart the PC (for a real cold start) or at least wait a few minutes, quit every Deskair, then run:

```powershell
$exe = (Resolve-Path artifacts\r2r-on\Deskair.App.exe).Path   # then r2r-off
$watch = [Diagnostics.Stopwatch]::StartNew()
$process = Start-Process $exe -ArgumentList '--simulated' -PassThru
while ($process.MainWindowHandle -eq 0) { Start-Sleep -Milliseconds 10; $process.Refresh() }
"window after $($watch.ElapsedMilliseconds) ms"
```

Take three runs of each and keep the middle one. Turn ReadyToRun on (one property in `Deskair.App.csproj` plus a decision entry) only if it saves more than about 100 ms for a few MB. Otherwise write the numbers into `decisions.md` as "measured, not worth it" so nobody measures again.

## 10. Not worth doing (checked 2026-10-22)

| Idea | Why not |
|---|---|
| Full trimming (`TrimMode=full`) | WPF uses reflection everywhere and is not supported trimmed |
| Removing `D3DCompiler_47_cor3.dll`, `Microsoft.DiaSymReader.Native.amd64.dll`, `mscordaccore*.dll`, `createdump.exe` | Needed by WPF or for crash dumps, or not removable in a supported way; about 10 MB in all |
| A framework-dependent package | There is no shared .NET 10 runtime package for MSIX (`store-packaging.md`) |
| Excluding the `--gallery` tool from Release builds | Small (part of the 785 KB `Deskair.App.dll`) and the render tests use it |
| Collapsing the platform interfaces or the sensor-provider model | They are the test seams and the fork model (`architecture.md`, `adding-a-sensor.md`) |
