# Store packaging: the plan, step by step

*Last reviewed: 2026-10-20*

Backlog item: REL-01. Status: **plan, waiting for the owner's decisions** (section 2). Written for the owner, who is new to Store publishing. Facts come from Microsoft's own pages (links at the end); anything not confirmed is marked **unverified**.

## 1. The big picture in six lines

1. The Microsoft Store only accepts a Windows desktop app as an **MSIX package**: one file that holds the app, a manifest (an XML "ID card") and its pictures.
2. You do **not** sign the package yourself for the Store. You upload it, and Microsoft signs it. So no code-signing certificate is needed for the Store route.
3. You need a **developer account** in Partner Center. For an individual it is free (since 2025: no fee, no credit card; a personal Microsoft account plus an ID and a selfie).
4. In Partner Center you **reserve the app name**. Reserving it gives you the two values (identity name and publisher) that go into the manifest.
5. You fill in the **listing** (name, description, screenshots, age rating, privacy policy link) and **submit**. Microsoft's certification checks the package and the listing.
6. After it is published you ship updates by uploading a new package with a higher version.

## 2. Decisions for the owner

| # | Decision | My recommendation |
|---|---|---|
| 1 | Where does packaging live, and do we use a `.wapproj`? | A folder `packaging/` at the repo root, **outside** `Deskair.sln`, and **no `.wapproj`** (see 3). |
| 2 | Account type | Individual (free). A company account costs a one-time $99 and is not needed. |
| 3 | Publisher display name | See section 9: a handle you also use on GitHub, or your name. Public, decide once. |
| 4 | App name | "Deskair" alone (see 6 for why nothing more goes in the title). |
| 5 | Privacy policy page | A `PRIVACY.md` in the GitHub repo, linked by URL. One honest paragraph: nothing leaves the PC. |
| 6 | Courtesy mail to the sensor maker | Optional, see 6. |
| 7 | `LICENSE` file (none yet) | MIT, before the listing says "open source". |

## 3. Where the packaging files go (the folder question)

Not a bad practice either way, but there is a technical reason to keep it apart.

| Option | What it is | Verdict |
|---|---|---|
| A. `.wapproj` project inside `src/` and in `Deskair.sln` | Visual Studio's "Windows Application Packaging Project". It is the route Microsoft documents for desktop apps that are not WinUI 3 ([Microsoft: package a desktop app](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-packaging-dot-net)). | It needs Visual Studio's own build tools. `dotnet build Deskair.sln` (our first check) would stop working or need special handling. **Not recommended in the solution.** |
| B. `.wapproj` in its own folder and its own `.sln` | Same project, but isolated: `packaging/Deskair.Package.sln`. | Works, but is one more new project type to maintain, and still needs Visual Studio. Fallback if C fails. |
| **C. Plain files in `packaging/`, no project** | `AppxManifest.xml` written by hand, the logo images, and one PowerShell script that runs `dotnet publish` and then `MakeAppx.exe` (a command-line tool from the Windows SDK) to produce the `.msix`. | **Recommended.** No new project, no change to project references, no Visual Studio needed, runs from the terminal (and later from GitHub Actions). Easy for you to read: it is one manifest and one script. |

Why `packaging/` and not `src/`: `src/` holds the code that goes into the app. The manifest, images and script are not code and are never compiled into it. A folder at the repo root is the common choice for installer and packaging files.

`AGENTS.md` says to ask before adding projects: option C adds none, so it needs only your agreement on the folder. Option B would need a yes.

## 4. Words you will meet

| Word | Meaning |
|---|---|
| MSIX | The package format. A zip with a manifest. |
| Manifest (`AppxManifest.xml`) | Says who the app is (identity), what it needs (capabilities), how it starts, which languages and logos it has. |
| Identity Name and Publisher | Two strings Partner Center gives you when you reserve the name. They must match the manifest, or the Store rejects the package. |
| Package identity | Having an identity makes Windows treat the app as "installed from a package". Toasts, startup and uninstall then work through the manifest instead of the registry. |
| Capability | A permission declared in the manifest. We need `bluetooth` (a device capability, so the Bluetooth sensor can be used) and `runFullTrust` (a normal desktop app, not sandboxed). |
| `runFullTrust` | A restricted capability: Partner Center asks you to explain why. For a classic desktop app the answer is simply "this is a Win32/.NET desktop app that cannot run without it". |
| Sideloading | Installing the package on your own PC without the Store, to test it. Needs a test certificate. |
| `.msixupload` | The file you upload to Partner Center (the package plus symbols; a plain `.msix` is also accepted). |

## 5. Phases

Each phase ends with something you can see. "Who" says whose hands are needed.

### Phase 0: accounts and the name (owner, about 30 minutes plus waiting)

| Step | Detail |
|---|---|
| 0.1 | Register an individual developer account at [storedeveloper.microsoft.com](https://storedeveloper.microsoft.com) with a personal Microsoft account. Identity check: an ID and a selfie. Verification can take a while (**unverified**: how long). |
| 0.2 | In Partner Center: *Apps and games, New product, MSIX or PWA app*, reserve the name **Deskair**. A reservation is held for a limited time (**unverified**: about 3 months); submit before it lapses. |
| 0.3 | Open the product's *Product identity* page and copy three values to me: `Package/Identity/Name`, `Package/Identity/Publisher` (looks like `CN=GUID`) and `Package/Properties/PublisherDisplayName`. These are not secret. |
| Done when | I have the three values. |

If "Deskair" is taken, we pick another name before any of the work below; nothing else depends on it.

### Phase 1: make the app aware of packaging (agent, small)

| Step | Detail |
|---|---|
| 1.1 | One place that answers "does the app have package identity?" (a Windows call). Everything below branches on that single answer. |
| 1.2 | Packaged: skip writing the toast identity to the registry (`ToastIdentity`) and skip the Run key (`StartupRegistration`). The manifest declares both instead (toast activation, startup task). Unpackaged builds (GitHub, winget, `dotnet run`) keep working as today. |
| 1.3 | Settings: "Start with Windows" must work in the packaged build too (a packaged startup task can be switched off by the user in Windows Settings; the toggle must show the truth). |
| 1.4 | Tests for the switch with fakes. |
| Done when | The unpackaged app behaves as before (all checks green) and the packaged path is covered by tests. |

### Phase 2: the manifest, the images and the build script (agent)

| Step | Detail |
|---|---|
| 2.1 | `packaging/AppxManifest.xml`: identity from phase 0, `bluetooth` and `runFullTrust`, languages `en-US` and `uk-UA`, toast activation (the same COM id the app uses today), startup task, entry point `Deskair.App.exe`. |
| 2.2 | Logos in the sizes the Store needs (square 44, 150, wide 310x150, store logo 50, and scaled variants), made from the existing app icon. |
| 2.3 | `packaging/build-msix.ps1`: `dotnet publish` (release, win-x64, **self-contained**, because there is no shared .NET 10 runtime package for MSIX; **unverified** and the size, probably 100 MB or more, will be measured) then `MakeAppx pack`. Needs the Windows SDK installed (**check**: is it on your PC?). |
| 2.4 | `packaging/README.md`: how to build, how to bump the version. |
| Done when | The script produces `Deskair_x.y.z.0_x64.msix` from a clean checkout. |

Version rule: the MSIX version has four numbers (`1.0.0.0`); the Store needs each upload to be higher than the last, and the last number is reserved by the Store (keep it `0`).

### Phase 3: install it on your own PC (both, about 30 minutes)

| Step | Detail |
|---|---|
| 3.1 | Agent: a script that makes a **test certificate** with the same publisher string and signs the package. |
| 3.2 | Owner: trust the test certificate once (an admin step on your PC; I will give exact commands and how to remove it afterwards), then double-click the `.msix`. |
| 3.3 | Owner checks in the installed app, same list as the manual checks: finds the sensor over Bluetooth; toast appears; Snooze and Open work, also when the app is closed; Start with Windows; data lands in `Documents\Deskair`; tray, flyout, sleep and wake; uninstall removes the app but not your data. |
| Done when | Every item works in the packaged install. If toasts fail, the Windows App SDK is the fallback (question Q8, I ask you first). |

This is the phase where Bluetooth and toasts are really proven. Nothing before it can show that.

### Phase 4: the listing (both)

| Item | Detail |
|---|---|
| Texts | Name, short and long description, what's new, search terms, in English and Ukrainian. Drafted by me, with the wording rules in section 6. |
| Screenshots | At least one, up to ten, 1366x768 or larger, per language. I render them from the app (light and dark, English and Ukrainian). Captions say what the screen is. |
| Category | Utilities and tools, or Health and fitness (**decide**). |
| Age rating | Questionnaire in Partner Center; answers are "no" to everything. |
| Privacy policy | URL of `PRIVACY.md`. Required for desktop apps by policy 10.5.1. |
| Support contact | The GitHub repository's issues page. |
| Hardware requirements | State the Aranet4 and Bluetooth LE requirement here too. |
| Pricing | Free. |
| Capability notes | The `runFullTrust` and Bluetooth explanations (section 4). |

### Phase 5: submit and certification (owner clicks, agent prepares)

| Step | Detail |
|---|---|
| 5.1 | Upload the package built from the final commit; fill the listing; submit. |
| 5.2 | Microsoft checks security, policy and the listing. Typical time: **unverified** (reports say one to a few days). Certification can fail; the report says why and we fix and re-upload. |
| 5.3 | Likely questions from certification: why full trust; why Bluetooth; "how do I test without the sensor?" Say in the notes to testers that the app needs an Aranet4 and that `--simulated` is not available to them, so include a short screen recording or a plain explanation. **Plan for this**: a reviewer without the sensor sees only the welcome screen, and that screen must make sense on its own (policy 10.1.1: the value must be clear on first run). |
| Done when | The listing is live and you can install it from the Store. |

### Phase 6: after release

| Step | Detail |
|---|---|
| 6.1 | Updates: bump the version, build, upload, submit. |
| 6.2 | GitHub release with the same package (needs a signing certificate if we want it installable outside the Store; **later**). Winget later. |
| 6.3 | Keep the privacy page and docs in step. |

## 6. Presenting an unofficial app (the naming question)

What the policies say (from [Microsoft Store Policies 7.20](https://learn.microsoft.com/en-us/windows/apps/publish/store-policies)):

| Rule | What it means for us |
|---|---|
| 10.1.1: metadata must not mislead about "functionality, or relationship to other products"; must state "important limitations ... including required or supported input devices" | We **must** say that Deskair works only with the Aranet4 and needs it, and that it is unofficial. |
| 10.1.1: "must not claim to be from a company ... if you do not have permission" | No wording that suggests the maker made or endorses it. |
| 10.1.1: the title "must not contain marketing or descriptive text, including extraneous use of keywords" | The title is **"Deskair"** only. No "Deskair for Aranet4". The compatibility goes in the first line of the description, the screenshots and the hardware requirement. |
| 10.1.3: search terms "not use other product titles unless those products are also published by you" | **Risk:** `Aranet4` as a search term could be read as another product's title. The description text will mention it anyway. I would use it in the description and captions and leave it out of the search terms field, or try it and remove it if certification objects (**decide**). The earlier plan in `release-and-distribution.md` said to add it as a search term; this is the reason to revisit. |
| 11.2: content must be your own, licensed, or "used as otherwise permitted by law" | No Aranet logo, no product photos from their site. Our own UI and our own icon only. Naming the product you work with, in plain text, to say what it is compatible with, is the normal practice (nominative use). |
| 10.5.1 | Privacy policy URL required. |

Wording to use (a draft, the final text comes in phase 4):

| Where | Text |
|---|---|
| First line of the description | "Unofficial, free desktop monitor for the Aranet4 CO₂ sensor. Requires an Aranet4 with Bluetooth." |
| Second paragraph | "Deskair is an independent open-source project. It is not made by, affiliated with or endorsed by the maker of Aranet4. Aranet is a trademark of its owner." |
| Screenshot captions | "Your Aranet4 on your PC", "CO₂ history from your Aranet4" |
| Hardware requirements field | "Aranet4 sensor and a PC with Bluetooth LE." |
| First-run screen | Already says what it needs; keep the Aranet4 line visible. |
| In the app, About or Settings | A short "Unofficial" line and the link to the repository. |

Do not: use the Aranet logo or colours; call it an "app for Aranet" in the title; say "official", "certified" or "approved"; imply any support from the maker.

**The maker.** Aranet appears to be a brand of SAF Tehnika (Latvia) (**unverified** from search results; check the company on their site). I found no published brand rules for third parties. A short courtesy email ("I am publishing a free, open-source, unofficial Windows reader for the Aranet4; here is the wording I plan to use; tell me if anything bothers you") costs nothing and gives a paper trail; it is not required by the Store. Your call.

Checked 2026-10-19 on the owner's sensor: with "Smart Home integration" off, reading the current values fails (`ProtocolError`), so it is required. The first-run steps, the offline steps and the listing say so (D-0026).

## 7. Order of work and who does what

| Order | Phase | Who | Blocked by |
|---|---|---|---|
| 1 | 0 accounts and name | owner | nothing, **start now** (it has waiting time) |
| 2 | 1 packaged-aware app | agent | nothing |
| 3 | 2 manifest, images, script | agent | phase 0 values, and the Windows SDK |
| 4 | 3 local install test | both | phase 2 |
| 5 | 4 listing | both | phase 3 results |
| 6 | 5 submit | owner | all above, plus the P1 answers (they are the listing texts) |

## 8. What is still unknown

| Question | How we find out |
|---|---|
| Is the Windows SDK (`MakeAppx.exe`) on your PC? | I check with one command. |
| Size of the self-contained package | Measured in phase 2. |
| Do toasts and Bluetooth work for the packaged app? | Phase 3. |
| Verification time for the account, certification time | Only the real process tells. |
| Is "Aranet4" acceptable as a search term? | Certification result; start without it. |

## 9. Answers to the owner's questions (2026-10-18) and status

A later agent: start here, then read sections 3, 5 and 6. Nothing in this section needs to be researched again.

| Topic | Answer |
|---|---|
| Status | Plan written. The owner has not yet registered a Partner Center account or reserved the name. Windows 11 SDK 10.0.26100 is installed (`makeappx.exe` found in `C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64`, 2026-10-18). The owner added the MIT `LICENSE`. GitHub user: `romanshevchuk`. Publisher name: see the advice below.

**Done 2026-10-18:** phase 0 (account "Roman Shevchuk", name "Deskair" reserved; identity `RomanShevchuk.Deskair`, publisher `CN=C6F2FCB9-D5F4-41BE-9616-7B99EB94EFA1`, Store ID `9N027HRZXSMH`, package family `RomanShevchuk.Deskair_0vc1xnb0gk962`); phase 1 (`IPackageIdentity`: a packaged run skips the toast registry entries and uses `PackagedStartupRegistration`, the startup task `DeskairStartup`; `ToastNotifier` uses the package identity); phase 2 (`packaging/`: manifest, logos, `build-msix.ps1` makes a 69 MB self-contained `.msix`, `sign-test.ps1` and `trust-test-cert.ps1` for phase 3). **Not done:** phase 3 (the owner installs the test package and checks toasts, Bluetooth, start with Windows), phases 4 and 5. **Not verified:** that the package installs and runs; the toast picture path (`ToastLogos` writes under `%LOCALAPPDATA%`, redirected in a packaged app, so toast pictures may not show; fix with an `ms-appdata` path if phase 3 shows it); the manifest was only checked by `makeappx`. Trademark checks: USPTO no result for "Deskair"; EUIPO TMview shows an expired Australian mark and "FLIPDESKAIR" (a different word), judged not a conflict by the owner. |
| Which Windows SDK | **Windows 11 SDK (10.0.26100.x)**. It matches the owner's Windows 11 25H2 (build 26200), is the current stable one, and contains `makeappx.exe` and `signtool.exe`. The 22621 one is older and 28000 is newer than needed. The app targets 10.0.19041, which is independent of this choice. After installing, check `C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.*\x64\makeappx.exe`. |
| "Search terms" | A Partner Center field with up to seven words or phrases that help Store search find the app, separate from the title and description. The Store also reads the description, so `Aranet4` in the first line of the description is indexed anyway (how much weight it gets is **unverified**). Google finds the listing page by its text and the GitHub repository by name, description, topics and README; the repository matters more for Google than the Store page. |
| Repository | Yes, update it when the app is published: description "Unofficial Windows monitor for the Aranet4 CO₂ sensor", topics `aranet4`, `co2`, `air-quality`, `bluetooth-le`, `wpf`, `windows`, a short "unofficial, not affiliated" line at the top of `README.md`, and a link to the Store listing. The repository is `romanshevchuk/deskair-aranet4`; the name is fine. **There is no `LICENSE` file yet:** an open-source release needs one (owner decision, MIT is the usual simple choice) before the listing says "open source". A `PRIVACY.md` is also needed (phase 4). |
| Support contact | Decided by the owner 2026-10-18: the GitHub issues page of `romanshevchuk/deskair-aranet4`; no email on the listing. |
| Toast pictures when packaged | Checked by the owner 2026-10-18 in the installed test package: the ring picture shows on the toast, so the `%LOCALAPPDATA%` redirect is not a problem. Taskbar icon, tray icon and the app also checked; install and removal worked. |
| Account and publisher name | See below. |

**Account and publisher name.** The legal name used for the identity check (ID and selfie) is private to Microsoft. The *publisher display name* is the public one on the listing. It must be unique in the Store and must not mislead; whether an individual account may use a name other than the legal name is **unverified** (check in the registration form). Options, best first:

1. A short developer handle that you also use on GitHub (for example the GitHub account name), so the Store, the repository and any later winget package point to one identity.
2. "Roman Shevchuk" as is: honest and simple, but common, so the name may already be taken or confusable.
3. A form with a distinguishing word, such as "Roman Shevchuk (Deskair)" or "R. Shevchuk".

The publisher name can be changed later only with effort (it is tied to the reserved name and the package identity), so decide once, before reserving "Deskair". A pseudonym is fine for a free hobby app; a company-style name is not needed.

**Advice given 2026-10-18 (small search; the evidence is thin).** Microsoft's own page says the display name is entered at registration, is at most 50 characters, is what customers see and "will come to know your apps by", and that you need the rights to the name (a trademarked name can get the account closed). Individual accounts are meant for hobby developers, and the account type cannot be changed later. I found no Microsoft rule that an individual must use the legal name, and no study showing that a real name or a handle changes downloads; one forum anecdote says a company name changed nothing. Whether the display name can be changed later without a verification step is **unverified**. The package identity `Publisher` is a separate `CN=...` string from Partner Center, so the display name does not affect the manifest. Recommendation: **"Roman Shevchuk"** (a real name next to a GitHub link is the trust signal that matters for a free open-source app); if it is taken, **"romanshevchuk"** (the same as the GitHub user). What builds trust is the rest of the listing: a link to the repository, the MIT licence, a privacy page, a support contact, honest screenshots.

## Sources

- [Free developer registration for individual developers on Microsoft Store (Windows Developer Blog, 2025-09-10)](https://blogs.windows.com/windowsdeveloper/2025/09/10/free-developer-registration-for-individual-developers-on-microsoft-store/)
- [Microsoft Store Policies, version 7.20](https://learn.microsoft.com/en-us/windows/apps/publish/store-policies): 10.1.1, 10.1.3, 10.5.1, 11.2
- [Package a desktop app from source code using Visual Studio (MSIX)](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-packaging-dot-net): the `.wapproj` route; single-project MSIX is for WinUI 3 only
- [App capability declarations](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations): restricted capabilities such as `runFullTrust`
- Project decisions: `docs/decisions.md` D-0003 (Store first, data in Documents), D-0010 (name); `docs/engineering/release-and-distribution.md`

## 10. Prepared 2026-10-19 (agent)

| Item | Where |
|---|---|
| Privacy policy text | `PRIVACY.md` (push it to GitHub before the listing links to it) |
| Unofficial line | `README.md` top, and Settings, About card (`AboutSettingsView`, English and Ukrainian) |
| Listing texts, fields, certification notes (English; Ukrainian draft) | [`store-listing.md`](store-listing.md) |
| Store screenshots (2400 x 1400, light and dark, English and Ukrainian, "Aranet4" in the footer; made by the render tests, `$env:DESKAIR_RENDER=1`) | `artifacts/store/en`, `artifacts/store/uk` (not in git). Only 3 Ukrainian ones: reuse or re-render more if the listing needs them |
| First-run screen | Now names the Aranet4 in the welcome text and step 1, so a reviewer without the sensor sees what the app needs (policy 10.1.1). The plan said it already did; it did not |
| Test package | `artifacts\msix\Deskair_0.1.1.0_x64.msix` (built from the current branch; the first Store upload may use any version at or above it) |

Not done by the agent: the real tray flyout screenshot (the render is 840 px wide, below the Store minimum; take one with the packaged app if wanted), and all Partner Center clicks.

**Phase 3 results (2026-10-19, installed `0.1.1.0`, owner):** passed: Bluetooth finds the sensor, toast with picture, Snooze and Open on a toast, tray, flyout, Start with Windows toggle and its Windows startup entry, `Documents\Deskair` data, About card and Ukrainian. **Phase 3 results (2026-10-20, owner):** 7 (start after sign-in) passed, but the window opened instead of staying in the tray (fixed by D-0027, needs a re-check on a new package); 9 (sleep and wake) passed; 11 (uninstall keeps `Documents\Deskair`) passed.
