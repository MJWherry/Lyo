# Lyo.Privacy

Redaction and sanitization for **free text**, **JSON**, and **XML**: emails, phones, payment-card-shaped numbers (Luhn) with optional **BIN allow/block lists**, **IBAN** (MOD-97),
heuristic **bank-account digit blocks**, **API key / secret** patterns (AWS access keys, GitHub PATs, `KEY=value` assignments) with optional **entropy** gating, **opt-in national /
tax ID packs** (US SSN shape, UK NINO, German Steuer-ID heuristic), URL query strings, IP addresses, **best-effort US street lines**, composable regex/literal rules, **allowlisted
literals** that must never be masked, **policy JSON** for ops-owned tuning, **SHA-256 policy fingerprints** for audit trails (no secret material in the hash), named **presets**,
and optional **`Lyo.Metrics.IMetrics`** instrumentation.

Targets **netstandard2.0** and **net10.0**. ASP.NET Core helpers live in **`Lyo.Privacy.AspNetCore`**.

This library supports **operational hygiene** (logs, exports, support tooling). It does **not** replace legal or compliance review.

## Project layout

Sources are grouped by area (similar to **`Lyo.Diagnostic`**). Namespaces are split (for example **`Lyo.Privacy.Abstractions`**, **`Lyo.Privacy.Rules`**, **`Lyo.Privacy.Json`**) so
feature boundaries stay explicit.

| Folder               | Contents                                                                                                                                                                                                            |
|----------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`Abstractions/`**  | `IRedactionRule`, `IRedactionMatchFormatter`, `RedactionSpan`, `RedactionResult`                                                                                                                                    |
| **`Enums/`**         | All public enums (`RedactionKind`, `JsonKeyRedactionStrategy`, `XmlScalarStrategy`, `PhoneMaskMode`, `EmailMaskStyle`, `IpRedactionMode`, `NationalIdPacks`, `ApiSecretPatterns`, …), each in its logical namespace |
| **`Policy/`**        | `RedactionPolicy`, `RedactionPolicyBuilder`, `RedactionPolicyFingerprint`, `PolicyJson`                                                                                                                             |
| **`Text/`**          | `TextRedactor`, `ITextRedactor`                                                                                                                                                                                     |
| **`Json/`**          | `JsonRedactor`, `JsonRedactorOptions`, `IStructuredRedactor`                                                                                                                                                        |
| **`Xml/`**           | `XmlRedactor`, `XmlRedactorOptions`                                                                                                                                                                                 |
| **`Configuration/`** | `PrivacyRedactorOptions`, `PrivacyPolicies` / `PrivacyPresetNames`                                                                                                                                                  |
| **`Metrics/`**       | `PrivacyMetricNames`, `PrivacyMetricsRecorder`                                                                                                                                                                      |
| **`Rules/`**         | Built-in and composable rules, mask option classes                                                                                                                                                                  |
| **`Internal/`**      | Helpers not in the public API surface                                                                                                                                                                               |

**Options** (`JsonRedactorOptions`, `XmlRedactorOptions`, `EmailMaskOptions`, `PhoneMaskOptions`) are **`sealed` classes** with **`{ get; set; }`** for configuration binding and
in-place tuning, consistent with packages like **`Lyo.Cache`**.

## Compliance stance

- **Not a DLP or lawful-basis tool.** Presets and rules are **pattern-based heuristics**. They do not prove removal of personal data, determine lawful grounds, or satisfy HIPAA
  Safe Harbor, GDPR pseudonymisation, or sector-specific definitions on their own.
- **Prefer structured fields.** Redact columns or JSON keys you classify as sensitive; use free-text rules only where unstructured data is unavoidable.
- **Placeholders vs partial masks.** Full placeholders minimise residual disclosure; partial masks trade risk for supportability—choose explicitly per sink and retention.
- **Stable hashes** in JSON (`HashStable`) are **not encryption**. Anyone with the salt and payload can correlate values; treat salts as secrets and rotation as a product decision.

## Performance

- **Text:** Per-code-unit work is **O(input length × rules)** in the worst case (each rule scans the string). Presets keep rule counts small; trim rules on hot paths.
- **Text:** Winning-rule tracking uses **parallel `int[]` / `RedactionKind[]` windows** (no per-position nullable types) to reduce overhead vs `int?`.
- **JSON:** `MemoryStream` is **pre-sized** from input length to cut reallocations; **`RedactJsonUtf8`** parses **`ReadOnlyMemory<byte>`** without building a Unicode string first (
  rules on string values still materialize strings when applied). **`RedactJsonStream`** buffers the input stream, redacts, then writes UTF-8 output.
- **Text span:** **`ITextRedactor.Redact(ReadOnlySpan<char>)`** routes through a single buffer copy, then the same engine as **`string`** (rules still operate on **`string`**
  today).
- **XML:** **`XmlRedactor`** walks **`XDocument`** by sensitive element local name; invalid XML can fall back to **`ITextRedactor`** like JSON.
- **Hashes:** Short hex prefixes for `HashStable` use a **fixed small `char[]`**, not per-nibble string allocations.

## Internationalization

- **Email:** The detector targets **ASCII-looking** addresses (common in logs). **IDN / punycode** domains (e.g. `xn--`) and internationalised local-parts may not match; consider
  normalising or using structured email fields.
- **Phone:** Patterns skew toward **NANP-style** grouping; international formats (short codes, different punctuation) may be missed or partially matched. Stricter scenarios may
  need locale- or country-specific rules (or validation libraries) in addition to this package.

## Core concepts

| Type                                   | Role                                                                                                                                                                           |
|----------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `IRedactionRule`                       | Finds spans in a string to replace.                                                                                                                                            |
| `IRedactionMatchFormatter`             | Optional: return custom text per match; if `null`, `RedactionPolicy.Placeholder` is used.                                                                                      |
| `RedactionPolicy`                      | Ordered rules + placeholder + whether adjacent runs merge.                                                                                                                     |
| `RedactionPolicyBuilder`               | Fluent composition; `AppendPreset` for built-ins.                                                                                                                              |
| `ITextRedactor` / `TextRedactor`       | Applies a policy to plain text.                                                                                                                                                |
| `IStructuredRedactor` / `JsonRedactor` | Rewrites JSON using sensitive-key strategies; UTF-8 helpers; optional string pass-through via `ITextRedactor`.                                                                 |
| `XmlRedactor` / `XmlRedactorOptions`   | Element local-name strategies (`Placeholder`, `RemoveElement`); optional text rules on non-sensitive text.                                                                     |
| `PolicyJson`                           | Deserialize **JSON** policy definitions into **`RedactionPolicy`** (YAML → convert to JSON externally).                                                                        |
| `RedactionPolicyFingerprint`           | **`ComputeSha256HexPrefix`**: checksum of policy **shape** + coarse rule options (not raw regex bodies or literals).                                                           |
| `PrivacyMetricNames`                   | Stable metric names when `IMetrics` is used (tags `kind`, optional `policy`).                                                                                                  |
| `RedactionResult`                      | **`ToString` / debugger views omit `Text`** so logs and inspectors stay safe; includes **`InputUtf16Length`**, **`OutputUtf16Length`**, **`PolicyName`**, **`HadRedactions`**. |

## `RedactionPolicyBuilder` helpers

Besides **`AddRule(IRedactionRule)`**, the builder exposes **`AddRule<T>(T rule) where T : class, IRedactionRule`** and shorthand methods such as **`AddPhoneRule(PhoneMaskMode, …)`
**, **`AddPhoneRule(PhoneMaskOptions, …)`**, **`AddEmailRule(…)`**, **`AddPaymentCardRule(allowedBins6, blockedBins6)`**, **`AddUrlRule()`**, **`AddIpRule(…)`**, *
*`AddAddressRule()`**, **`AddIbanRule()`**, **`AddBankAccountRule(…)`**, **`AddNationalIdRule(…)`**, **`AddApiSecretRule(…)`**, **`AddLiteralRule(…)`**, and **`AddRegexRule(…)`**.
Extension **`AppendPreset`** lives in **`Lyo.Privacy.Configuration`**.

## Email masking (`EmailRedactionRule`, `EmailMaskOptions`)

Use **`EmailMaskOptions`** (or the legacy **`EmailMaskStyle`** ctor, which maps to the same shapes). When **`UsePolicyPlaceholder`** is true, the whole address becomes the policy *
*`Placeholder`**.

| What you configure                                             | Example options                                                                                                    | Input                             | Output                     |
|----------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------|-----------------------------------|----------------------------|
| Full placeholder                                               | `EmailMaskOptions.PolicyPlaceholder` or `new EmailRedactionRule()`                                                 | `Contact alice@example.com today` | `Contact [redacted] today` |
| Partial local (factory)                                        | `EmailMaskOptions.PartialLocalPreserveDomain(1)`                                                                   | `alice@example.com`               | `a***@example.com`         |
| First local character + full domain                            | `VisibleLocalPrefixLength = 1`, `PreserveEntireDomainHost = true`                                                  | `alice.wonder@example.com`        | `a***@example.com`         |
| First local character + mask first domain label, keep the rest | `VisibleLocalPrefixLength = 1`, `PreserveDomainFromFirstDot = true`, `VisibleDomainPrefixLength = 0`               | `u@mail.example.co.uk`            | `u***@***.example.co.uk`   |
| Show start of first domain label                               | Same as row above plus `VisibleDomainPrefixLength = 2`                                                             | `u@mail.example.co.uk`            | `u***@ma***.example.co.uk` |
| Drop `@`, custom join (local mask stays `***`)                 | `PreserveAtSign = false`, `AtReplacement = "#"`, `PreserveEntireDomainHost = true`, `VisibleLocalPrefixLength = 1` | `alice@mail.example.com`          | `a***#mail.example.com`    |
| Plus address tagging                                           | `VisibleLocalPrefixLength = 1`, `PreserveEntireDomainHost = true`, default `PlusTagMaskLiteral`                    | `shop+promo@example.com`          | `s***+***@example.com`     |

**Useful fields:** **`VisibleLocalSuffixLength`**, **`LocalMaskLiteral`**, **`PreserveLocalSeparators`** / **`SeparatorMaskChar`** (for `.` / `-` / `_` beside masked segments), *
*`VisibleDomainSuffixLength`**, **`DomainMaskLiteral`**, **`PreserveEntireDomainHost`**, **`PreserveDomainFromFirstDot`**, **`AtReplacement`** (only when **`PreserveAtSign`** is
false; if unset, **`AtReplacement`** falls back to **`LocalMaskLiteral`**).

## Phone masking (`PhoneRedactionRule`, `PhoneMaskOptions`)

Digits are counted in order through the match (**country code and groups collapse to one digit stream**). **`LeadingDigitsVisible`** and **`TrailingDigitsVisible`** are a **union
** (if they overlap, every digit can become visible). Set **`OnlyFirstDigitAmongLastN`** to ignore those two counts and show a single digit in the last *N* window.

| What you configure                        | Example options                                                                         | Input (snippet)   | Output (matched phone only)                                                                                   |
|-------------------------------------------|-----------------------------------------------------------------------------------------|-------------------|---------------------------------------------------------------------------------------------------------------|
| Placeholder                               | `PhoneMaskOptions.PolicyPlaceholder`                                                    | `+1-555-123-4567` | `[redacted]`                                                                                                  |
| Last four (factory)                       | `PhoneMaskOptions.LastFourDigits(digitsOnly: true)`                                     | `555-123-4567`    | `*******4567`                                                                                                 |
| Last four digits, digits only             | `TrailingDigitsVisible = 4`, `DigitsOnlyOutput = true`                                  | `+1-555-123-4567` | `*******4567`                                                                                                 |
| First digit + last two, digits only       | `LeadingDigitsVisible = 1`, `TrailingDigitsVisible = 2`, `DigitsOnlyOutput = true`      | `+1-555-123-4567` | `1********67`                                                                                                 |
| First digit of the last four, digits only | `OnlyFirstDigitAmongLastN = 4`, `DigitsOnlyOutput = true`, `PreserveSeparators = false` | `+1-555-123-4567` | `*******4***`                                                                                                 |
| Last four, keep separators                | `TrailingDigitsVisible = 4`, `PreserveSeparators = true` (default)                      | `+1-555-123-4567` | Separators stay when they sit next to a visible digit; other digit positions use **`MaskChar`** (default `*`) |

The legacy **`PhoneMaskMode`** ctor still works: **`Full`** → placeholder; **`LastDigits`** → **`TrailingDigitsVisible`** (separators preserved by default); *
*`FirstDigitOfLastGroup`** → **`OnlyFirstDigitAmongLastN`** with **`DigitsOnlyOutput`** and without separators.

## Other built-in text rules (quick examples)

| Rule                                          | Typical behaviour                                                                    | Example                                                                         |
|-----------------------------------------------|--------------------------------------------------------------------------------------|---------------------------------------------------------------------------------|
| `PaymentCardRedactionRule`                    | Luhn-only runs; often keep last four; optional **`AllowedBin6`** / **`BlockedBin6`** | `PAN 4111111111111111 ok` → `PAN [redacted]1111 ok`                             |
| `UrlRedactionRule`                            | Strip query string                                                                   | `https://api.example/v1?id=1&token=secret` → `https://api.example/v1[redacted]` |
| `IpAddressRedactionRule`                      | Depends on **`IpRedactionMode`**                                                     | Truncate last segment: `203.0.113.44` → `203.0.113.[redacted]`                  |
| `AddressRedactionRule`                        | US-style street line (**noisy** in prose)                                            | Matched line → **`Placeholder`**                                                |
| `LiteralSubstringRedactionRule`               | Exact / case-insensitive substring                                                   | As configured                                                                   |
| `RegexRedactionRule`, `DelegateRedactionRule` | Custom detection; optional per-match formatter                                       | As configured                                                                   |
| `IbanRedactionRule`                           | IBAN-shaped token + **MOD-97**                                                       | Valid IBAN → full match masked                                                  |
| `BankAccountNumberRedactionRule`              | 8–19 digit **word-boundary** runs; optional **`MinNumericValue`**                    | Reduces tiny-number noise                                                       |
| `NationalIdRedactionRule`                     | **`NationalIdPacks`**: US SSN, UK NINO, DE Steuer-ID (heuristic)                     | Opt-in per pack                                                                 |
| `ApiSecretRedactionRule`                      | **`ApiSecretPatterns`**: AWS key, GitHub PAT, high-entropy `KEY=value`               | Optional **`MinEntropyBitsPerChar`** on values                                  |

`RegexRedactionRule` and `DelegateRedactionRule` accept optional `Func<string, RedactionSpan, string?>?` formatters.

**`CompositeRedactionRule`** does not forward inner formatters; composite matches use the policy placeholder unless you add a custom composite formatter later.

## Allowlisted literals (`RedactionPolicy.NeverRedactSubstrings`)

Substrings listed on the policy (or **`RedactionPolicyBuilder.WithNeverRedactSubstrings`**) **clear** winning redaction spans wherever they appear—useful for staging markers,
known-safe tokens, or fixed test data you must not destroy in logs.

## Policy as data (`PolicyJson`)

Load **`PolicyDefinitionDto`**-shaped JSON via **`PolicyJson.Build(json, configure?)`**. Rule kinds include **`email`**, **`phone`**, **`paymentCard`** (with **`allowedBins`** / *
*`blockedBins`**), **`iban`**, **`bankAccount`** (`bankMinNumeric`), **`nationalId`** (`nationalIdPacks`), **`apiSecret`** (`apiPatterns`, `apiMinEntropy`), **`literal`**, *
*`regex`**, plus URL / IP / address. Top-level **`neverRedactSubstrings`** maps to the allowlist above.

## Audit fingerprint (`RedactionPolicyFingerprint`)

**`RedactionPolicyFingerprint.ComputeSha256HexPrefix(policy)`** hashes **policy name**, **placeholder**, **merge flag**, **never-redact strings**, rule **order/types**, and *
*coarse option summaries** (BIN sets, API pattern flags + entropy threshold, national ID packs, phone/email option shape). It intentionally avoids embedding raw secrets: **regex
rules** contribute a **hash code** of the pattern and options, not the pattern text.

## `JsonRedactor` contracts

- If **`ApplyTextRulesToAllStringValues`** is true, an **`ITextRedactor`** must be passed to the constructor (otherwise `ArgumentException`).
- If JSON **parse fails** and no **`ITextRedactor`** was supplied for fallback, **`InvalidOperationException`** is thrown (inner exception is a `JsonException`). With a text
  redactor, invalid JSON is redacted as raw text.
- **`RedactJsonUtf8(ReadOnlyMemory<byte>)`** and **`RedactJsonStream(Stream, Stream)`** use the same rewrite pipeline as **`RedactJson`** (stream helper buffers the entire input
  first).

## XML (`XmlRedactor`)

Configure **`XmlRedactorOptions.SensitiveElementLocalNames`** (local name → **`XmlScalarStrategy.Placeholder`** or **`RemoveElement`**). Counts use **`RedactionKind.XmlSensitive`
**. Set **`ApplyTextRedactorToNonSensitiveText`** and pass an **`ITextRedactor`** to run text rules inside non-listed elements. Invalid XML can **fallback** to the text redactor,
recorded as **`lyo.privacy.xml.fallback_to_text`**.

## Presets (`PrivacyPolicies`, `PrivacyPresetNames`)

- **Minimal** — no built-in rules unless you add them.
- **Logging** — email (placeholder), **partial phone** (last 4 digits visible), cards, URL queries, truncated IPv4.
- **SupportExport** — logging core plus Bearer and JWT-shaped tokens.
- **PublicSurface** — full placeholder phone, cards, URLs, full IP, email placeholder, **address rule**.
- **RegressionTesting** — same rule mix as logging with placeholder `[REDACTED]` for snapshots.

## Metrics (`IMetrics`)

Pass an `Lyo.Metrics.IMetrics` implementation into `TextRedactor` / `JsonRedactor`, or register **`IMetrics`** in DI when using `AddLyoPrivacy` (resolved via
`GetService<IMetrics>() ?? NullMetrics.Instance`).

Counters use tag **`kind`** for `RedactionKind`. When a policy has **`RedactionPolicy.Name`** set (built-in presets do this automatically; use *
*`RedactionPolicyBuilder.WithPolicyName`**, **`PrivacyRedactorOptions.PolicyName`**, or **`JsonRedactorOptions.PolicyName`**), tag **`policy`** is added so dashboards can split *
*Logging** vs **PublicSurface**, etc. Keyed DI registration sets **`Name`** from the service key when still null.

| Name                                  | Kind                                                   |
|---------------------------------------|--------------------------------------------------------|
| `lyo.privacy.text.operations`         | Counter per `Redact` call                              |
| `lyo.privacy.text.duration`           | Timing per text call                                   |
| `lyo.privacy.text.redaction_runs`     | Counter for total merged redaction runs                |
| `lyo.privacy.text.redactions.by_kind` | Counter by `RedactionKind` (tag `kind`)                |
| `lyo.privacy.json.operations`         | Counter per `RedactJson` call                          |
| `lyo.privacy.json.duration`           | Timing per JSON call                                   |
| `lyo.privacy.json.key_redactions`     | Counter for JSON key-based replacements                |
| `lyo.privacy.json.redactions.by_kind` | Counter by kind (tag `kind`)                           |
| `lyo.privacy.json.fallback_to_text`   | Counter when invalid JSON falls back to text redaction |
| `lyo.privacy.xml.operations`          | Counter per `RedactXml` call                           |
| `lyo.privacy.xml.duration`            | Timing per XML call                                    |
| `lyo.privacy.xml.redactions.by_kind`  | Counter by `RedactionKind` in XML                      |
| `lyo.privacy.xml.fallback_to_text`    | Counter when invalid XML falls back to text redaction  |

## Example (manual)

```csharp
using Lyo.Metrics;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Rules;
using Lyo.Privacy.Text;

var policy = new RedactionPolicyBuilder()
    .AddEmailRule(EmailMaskStyle.PartialLocalPreserveDomain, visibleLocalPrefixLength: 1)
    .AddPhoneRule(PhoneMaskOptions.LastFourDigits(digitsOnly: true))
    .Build();

var redactor = new TextRedactor(policy, NullMetrics.Instance);
var result = redactor.Redact("Call +1-555-123-4567 or email alice@example.com");
```

Finer control uses the same rule types with **`EmailMaskOptions`** / **`PhoneMaskOptions`**; see the tables above.

## ASP.NET Core

```csharp
using Lyo.Privacy;
using Lyo.Privacy.AspNetCore;

services.AddLyoPrivacy(configuration);
```

Configuration section: **`Privacy`** (`PrivacyRedactorOptions.SectionName`). Register **`IMetrics`** (e.g. `MetricsService`) if you want redaction metrics recorded.

Keyed policy:

```csharp
services.AddLyoPrivacyPolicy("support", b => b.AddPolicy(PrivacyPolicies.SupportExport()));
```

## Related

- **`Lyo.Diagnostic`** — stack trace and path sanitisation for observability (separate from field-level PII rules).
