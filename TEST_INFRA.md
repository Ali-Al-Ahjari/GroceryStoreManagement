# E2E Test Infrastructure & Test Case Inventory (`LicenseIssuerCSharp`)

This document defines the testing philosophy, feature inventory, test architecture, and explicit coverage test suites for the End-to-End (E2E) testing framework of the `LicenseIssuerCSharp` application.

---

## 1. Test Philosophy

Our E2E testing framework is designed around two core principles:
1. **Opaque-Box Testing**: The application is tested strictly from the outside. The test runner has no internal access to WPF object instances, runtime memory, or private methods. It interacts with the application solely through its public CLI boundaries, its GUI Automation peers, and its SQLite database persistence layer.
2. **Requirement-Driven Verification**: Every test case directly traces to a requirement defined in `ORIGINAL_REQUEST.md` (R1: Licensing Engine, R2: User Interface, R3: Keypair Generator, R4: Database & Persistence) and the CLI contract defined in `PROJECT.md`.

---

## 2. Feature Inventory

The testing framework covers the following system features:
- **Dashboard View**: Displays metrics for total, active, and expired licenses issued.
- **Issue License View**: Client Name textbox, Machine Fingerprint normalization, Expiry Days defaulting, "Issue License" trigger, and "Copy Code" clipboard actions.
- **History View**: Scrollable record grid tracking client name, machine fingerprint, and expiration.
- **Settings View**: Load external private key file (.pem), generate 3072-bit key pair.
- **Keypair Generator**: Generates new 3072-bit RSA keys and saves them in PEM format.
- **Database & Persistence**: Local SQLite file (`%APPDATA%\LicenseIssuerCSharp\licenses.db`) containing historically issued license tokens.
- **Command-Line Interface (CLI)**:
  - `--issue` flag (with `--machine`, `--days`, `--key`, and `--issuer`)
  - `--verify` flag (with `--token`, `--machine`, and `--pubkey`)
  - `--generate-keys` flag (with `--out`)

---

## 3. Test Architecture

The E2E test runner is implemented in Python, leveraging a robust framework that drives both CLI and GUI testing:

```
                  +----------------------------------+
                  |        Python Test Runner        |
                  +-------+------------------+-------+
                          |                  |
        (CLI Automation)  |                  |  (GUI Automation via
     Invokes subprocess   |                  |   Windows UIAutomation)
                          v                  v
            +-------------+----+      +------+-------------+
            | CLI Mode         |      | GUI Mode           |
            | - Issue Token    |      | - Dashboard Views  |
            | - Verify Token   |      | - Forms & Inputs   |
            | - Generate Keys  |      | - History Grid     |
            +-------------+----+      +------+-------------+
                          |                  |
                          |                  |
                          +--------+---------+
                                   |
                                   v
                  +----------------+-----------------+
                  | SQLite DB: licenses.db           |
                  | Crypto Keys: PEM Files           |
                  +----------------------------------+
```

### 3.1. CLI Execution
CLI tests use Python's `subprocess` library to execute `LicenseIssuerCSharp.exe`.
- **Invoker**: `subprocess.run(args, capture_output=True, text=True, timeout=10)`
- **Verification**: The runner verifies the exit code, compares standard output (`stdout`) against expected patterns or regex, and inspects standard error (`stderr`) for correct diagnostic logs.

### 3.2. GUI Automation
GUI tests automate the WPF window using Windows UI Automation APIs.
- **Invoker**: System UI Automation library to locate controls by their `AutomationId`.
- **Inputs**: Setting values for `TxtClientName`, `TxtMachineFingerprint`, and `TxtExpiryDays`. Clicking buttons like `BtnIssueLicense` and `BtnCopyCode`.
- **Verification**: Verifying text outputs inside text blocks, inspecting clipboard contents using `tkinter` or `pyperclip` in Python, and navigating between view tabs (`BtnDashboard`, `BtnIssueLicense`, `BtnHistory`, `BtnSettings`).

### 3.3. Database Interrogation
Database tests directly read the SQLite database at `%APPDATA%\LicenseIssuerCSharp\licenses.db`.
- **Verification**: Python's `sqlite3` library is used to run queries against the database after actions are performed, validating that the data layer remains consistent with the UI and CLI state.

### 3.4. Cryptographic Compatibility Verification
To ensure seamless interoperability between `LicenseIssuerCSharp` and `GroceryStoreManagement`, the testing infrastructure incorporates strict cryptographic compatibility tests:
- **Default Key Verification**: The test runner issues a license token using the application's default embedded private key. It then verifies this token against the store's public key located at `GroceryStoreManagement/Security/default_license_public_key.pem` using a Python script built on the `cryptography` library.
- **Compatibility Helper Verification**: The runner uses a Python script (the "Compatibility Helper") with the default private key to generate a license token. It then executes the application's CLI verify mode (`--verify`) passing this token and specifying the store's public key to confirm that the C# application successfully accepts it.

---

## 4. Coverage Thresholds

To achieve "production-ready" test status, the suite enforces the following minimum coverage rules:
- **Tier 1: Feature Coverage**: Verify all individual CLI commands and GUI views operate correctly. **Threshold: >= 25 test cases.**
- **Tier 2: Boundary & Corner Cases**: Stress input limits, malformed formatting, path errors, and clock tampering. **Threshold: >= 25 test cases.**
- **Tier 3: Cross-Feature Combinations**: Validate workflows involving multiple components (e.g. generating keys, issuing with them, and verifying). **Threshold: >= 5 test cases.**
- **Tier 4: Real-World Application Scenarios**: Complex, multi-step customer deployment and renewal flows mimicking actual operations. **Threshold: >= 5 test cases.**

---

## 5. Detailed Test Case Inventory

Here is the complete inventory of 66 test cases. Every test case is explicitly defined with inputs, expected output patterns, and exit codes.

### Tier 1: Feature Coverage (27 Test Cases)

| Test ID | Test Name | Target | Description | Inputs / CLI Flags / GUI Actions | Expected Output Pattern | Expected Exit Code |
|---|---|---|---|---|---|---|
| **TC-T1-01** | CLI Issue - Default Key | CLI | Issue a license using the embedded default private key. | `--issue --machine "9A8B7C6D"` | Regex: `^[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$` (valid base64url token). | 0 |
| **TC-T1-02** | CLI Issue - Custom Issuer | CLI | Issue a license with a custom issuer string. | `--issue --machine "9A8B7C6D" --issuer "MainOrchestrator"` | A valid token where the decoded payload has `"issuer":"MainOrchestrator"`. | 0 |
| **TC-T1-03** | CLI Issue - Custom Expiry Days | CLI | Issue a license with custom day duration. | `--issue --machine "9A8B7C6D" --days 90` | A valid token where the decoded payload `"exp"` date is exactly 90 days from now. | 0 |
| **TC-T1-04** | CLI Issue - Custom Private Key | CLI | Issue a license using an external private key file. | `--issue --machine "9A8B7C6D" --days 30 --key "./test_private_key.pem"` | A valid token signed with the custom private key. | 0 |
| **TC-T1-05** | CLI Verify - Valid Token | CLI | Verify a valid token using the default public key. | `--verify --token "<VALID_DEFAULT_TOKEN>" --machine "9A8B7C6D"` | Substring: `Valid` or `Verification Succeeded`. | 0 |
| **TC-T1-06** | CLI Verify - Invalid Signature | CLI | Verify a token whose signature part was tampered. | `--verify --token "<VALID_DEFAULT_TOKEN_MODIFIED_SIGNATURE>" --machine "9A8B7C6D"` | Substring: `invalid` or `signature validation failed`. | 1 |
| **TC-T1-07** | CLI Verify - Wrong Fingerprint | CLI | Verify a token against a non-matching fingerprint. | `--verify --token "<VALID_DEFAULT_TOKEN>" --machine "WRONG_FINGERPRINT"` | Substring: `هذا الكود ليس لهذا الجهاز` or `fingerprint mismatch`. | 1 |
| **TC-T1-08** | CLI Verify - Expired Token | CLI | Verify a token whose expiration time has passed. | `--verify --token "<EXPIRED_TOKEN>" --machine "9A8B7C6D"` | Substring: `منتهي الصلاحية` or `expired`. | 1 |
| **TC-T1-09** | CLI Verify - Custom Pubkey | CLI | Verify a custom token using a custom public key. | `--verify --token "<CUSTOM_TOKEN>" --machine "9A8B7C6D" --pubkey "./test_public_key.pem"` | Substring: `Valid` or `Verification Succeeded`. | 0 |
| **TC-T1-10** | CLI Key Generation | CLI | Generate a 3072-bit key pair. | `--generate-keys --out "./new_keys"` | Substring: `private_key.pem` and `public_key.pem` generated. | 0 |
| **TC-T1-11** | GUI Dashboard - Metrics Zero | GUI | Verify the dashboard metrics displays 0 counts initially. | Open GUI, view Dashboard View | UI elements `TxtTotalLicenses` = "0", `TxtActiveLicenses` = "0", `TxtExpiredLicenses` = "0". | N/A |
| **TC-T1-12** | GUI Dashboard - Metrics Increment | GUI | Verify dashboard metrics increment after issuing a license. | Issue active license via UI, open Dashboard View | `TxtTotalLicenses` = "1", `TxtActiveLicenses` = "1", `TxtExpiredLicenses` = "0". | N/A |
| **TC-T1-13** | GUI Issue - Expiry Default | GUI | Verify Expiry Days field default value. | Open GUI, navigate to Issue License View | Textbox `TxtExpiryDays` text is "30". | N/A |
| **TC-T1-14** | GUI Issue - Valid License | GUI | Verify license generation with all valid GUI inputs. | Client Name: "Store Owner A", Machine Fingerprint: "FINGERPRINT1", Expiry Days: "30", Click `BtnIssueLicense` | Textbox `TxtActivationToken` is populated with a valid token string. | N/A |
| **TC-T1-15** | GUI Issue - Copy Button | GUI | Verify "Copy Code" button copies token to system clipboard. | Issue license via GUI, click `BtnCopyCode` | System clipboard contains the generated token. | N/A |
| **TC-T1-16** | GUI Issue - Empty Client Name | GUI | Verify license generation allows an empty client name. | Client Name: "", Machine Fingerprint: "FINGERPRINT1", Expiry Days: "30", Click `BtnIssueLicense` | Token is generated, saved to database with empty name. | N/A |
| **TC-T1-17** | GUI Issue - Uppercase Normalization | GUI | Verify machine fingerprint input gets normalized. | Machine Fingerprint: "  fingerprint-1-abc  ", Click `BtnIssueLicense` | Token payload machine field contains "FINGERPRINT-1-ABC". | N/A |
| **TC-T1-18** | GUI History - Grid Populated | GUI | Verify history list gets populated with issued licenses. | Navigate to History View after issuing license | Grid `DataGridHistory` contains a row with "Store Owner A", "FINGERPRINT1", and the correct expiry date. | N/A |
| **TC-T1-19** | GUI History - Grid Scrollability | GUI | Verify scrollable grid behaves correctly with many records. | Issue 15 licenses via GUI, navigate to History View | Scrollbar in `ScrollViewerHistory` is visible and enabled. | N/A |
| **TC-T1-20** | GUI Settings - Key Generation | GUI | Verify key generation from the Settings tab. | Settings View -> Click `BtnGenerateKeyPair` -> Choose directory "./ui_keys" | Private and public key PEM files are written into `./ui_keys`. | N/A |
| **TC-T1-21** | GUI Settings - Load Private Key | GUI | Verify loading an external private key in the settings tab. | Settings View -> Browse and Load `private_key.pem` -> Go to Issue Tab -> Issue License | Token is issued and signed using the loaded custom private key. | N/A |
| **TC-T1-22** | DB - Schema Initialized | DB | Verify local SQLite database schema is created on startup. | Launch application | File `%APPDATA%\LicenseIssuerCSharp\licenses.db` exists with `licenses` table. | 0 |
| **TC-T1-23** | DB - License Record Saved | DB | Verify that issued licenses are saved in the database. | Issue license via CLI or GUI | SQLite DB table `licenses` has a row matching the issued token. | 0 |
| **TC-T1-24** | CLI - Combined Option Run | CLI | Verify CLI handles issuer, key, and days options simultaneously. | `--issue --machine "9A8B7C6D" --days 45 --issuer "Global" --key "./test_private_key.pem"` | Valid token printed, signed with custom key, issuer is "Global". | 0 |
| **TC-T1-25** | Crypto - Default Key Compatibility | Crypto | Verify default private key token verifies against store's public key. | Use Python to verify token generated via CLI (default key) against `GroceryStoreManagement/Security/default_license_public_key.pem` | Verification success in Python script. | 0 |
| **TC-T1-26** | Crypto - Compatibility Helper | Crypto | Verify token issued by Python helper is verified by C# executable. | Generate token via Python using default private key, then run `LicenseIssuerCSharp.exe --verify --token "<TOKEN>" --machine "9A8B7C6D" --pubkey "GroceryStoreManagement/Security/default_license_public_key.pem"` | Substring: `Valid` or `Verification Succeeded`. | 0 |
| **TC-T1-27** | CLI - Help command | CLI | Verify passing help arguments shows application help text. | `--help` or `-h` | Help usage information and flags are listed in stdout. | 0 (or non-zero depending on design) |

---

### Tier 2: Boundary & Corner Cases (27 Test Cases)

| Test ID | Test Name | Target | Description | Inputs / CLI Flags / GUI Actions | Expected Output Pattern | Expected Exit Code |
|---|---|---|---|---|---|---|
| **TC-T2-01** | CLI Issue - 0 Expiry Days | CLI | Issue license with 0 duration. | `--issue --machine "9A8B7C6D" --days 0` | Token generated successfully; expiration matches issue date. | 0 |
| **TC-T2-02** | CLI Issue - Negative Expiry Days | CLI | Issue license with past duration. | `--issue --machine "9A8B7C6D" --days -10` | Token generated successfully; expiration is set 10 days in the past. | 0 |
| **TC-T2-03** | CLI Issue - Extremely Large Days | CLI | Issue license for a duration of 100 years. | `--issue --machine "9A8B7C6D" --days 36500` | Token generated successfully; year is set to 2126. | 0 |
| **TC-T2-04** | CLI Issue - Empty Fingerprint | CLI | Issue license with empty machine. | `--issue --machine ""` | Error: machine fingerprint cannot be empty. | Non-zero |
| **TC-T2-05** | CLI Issue - Whitespace Fingerprint | CLI | Issue license with space-heavy fingerprint. | `--issue --machine "  9A8B  7C6D  "` | Valid token with normalized fingerprint payload `"9A8B7C6D"`. | 0 |
| **TC-T2-06** | CLI Issue - Invalid Key Path | CLI | Issue license using a non-existent private key file. | `--issue --machine "9A8B7C6D" --days 30 --key "./non_existent.pem"` | Error: Private key file not found. | Non-zero |
| **TC-T2-07** | CLI Issue - Malformed PEM Key | CLI | Issue license with an invalid/corrupt PEM key file. | `--issue --machine "9A8B7C6D" --days 30 --key "./corrupted.pem"` | Error: Invalid private key format. | Non-zero |
| **TC-T2-08** | CLI Verify - Empty Token | CLI | Verify empty token string. | `--verify --token "" --machine "9A8B7C6D"` | Error: Token cannot be empty. | Non-zero |
| **TC-T2-09** | CLI Verify - Malformed Dot Format | CLI | Verify a token without a dot separator. | `--verify --token "nodottoken" --machine "9A8B7C6D"` | Error: Invalid token format. | Non-zero |
| **TC-T2-10** | CLI Verify - Bad Char in Payload | CLI | Verify token containing invalid characters in payload. | `--verify --token "p#y.sig" --machine "9A8B7C6D"` | Error: Invalid base64 URL format. | Non-zero |
| **TC-T2-11** | CLI Verify - Bad Char in Signature | CLI | Verify token containing invalid characters in signature. | `--verify --token "pay.s@g" --machine "9A8B7C6D"` | Error: Invalid base64 URL format. | Non-zero |
| **TC-T2-12** | CLI Verify - Empty Fingerprint | CLI | Verify token against empty fingerprint. | `--verify --token "<VALID_TOKEN>" --machine ""` | Error: Machine fingerprint cannot be empty. | Non-zero |
| **TC-T2-13** | CLI Verify - Invalid Pubkey Path | CLI | Verify token with non-existent public key path. | `--verify --token "<VALID_TOKEN>" --machine "9A8B7C6D" --pubkey "./non_existent.pem"` | Error: Public key file not found. | Non-zero |
| **TC-T2-14** | CLI Verify - Malformed Pubkey PEM | CLI | Verify token with malformed public key PEM file. | `--verify --token "<VALID_TOKEN>" --machine "9A8B7C6D" --pubkey "./corrupt_pub.pem"` | Error: Invalid public key format. | Non-zero |
| **TC-T2-15** | CLI Generate - Deep Path Dir | CLI | Generate key pair where directories must be created. | `--generate-keys --out "./non_existent_folder/subfolder"` | Folders are created and keys written successfully. | 0 |
| **TC-T2-16** | CLI Generate - Access Denied Dir | CLI | Generate key pair inside directory with restricted access. | `--generate-keys --out "C:\Windows\System32\restricted"` | Error: Access to path is denied. | Non-zero |
| **TC-T2-17** | GUI Issue - Giant Client Name | GUI | Issue license with a client name of 10,000 characters. | Client Name: 10,000 characters, Machine: "9A8B7C6D", Click `BtnIssueLicense` | UI stays responsive, token generated and saved without overflow. | N/A |
| **TC-T2-18** | GUI Issue - Non-Numeric Expiry | GUI | Issue license with alphabetical days input. | Client Name: "A", Machine: "9A8B7C6D", Expiry Days: "abc", Click `BtnIssueLicense` | Warning message shown, input is rejected. | N/A |
| **TC-T2-19** | GUI Issue - Float Expiry Days | GUI | Issue license with float days input. | Client Name: "A", Machine: "9A8B7C6D", Expiry Days: "30.5", Click `BtnIssueLicense` | Textbox either blocks '.' input or sanitizes value to 30. | N/A |
| **TC-T2-20** | GUI Settings - Malformed Key Load | GUI | Load corrupt PEM file as private key. | Settings -> Load "./corrupt.pem" | Warning alert: Invalid private key. Fallback key remains active. | N/A |
| **TC-T2-21** | GUI Settings - Key Overwrite | GUI | Generate keys in folder containing existing keys. | Settings -> Click Generate -> Choose folder with existing keys | Dialog warns user of overwrite, or completes overwrite. | N/A |
| **TC-T2-22** | DB - SQL Injection Client Name | DB | Attempt SQL injection in client name field. | Client Name: "'; DROP TABLE licenses; --", Machine: "9A8B7C6D", click "Issue" | Name saved literally as string; database schema remains intact. | N/A |
| **TC-T2-23** | DB - Locked DB File Handling | DB | Attempt license issue while SQLite database file is locked. | Open database file and acquire exclusive lock via external tool, then click "Issue" | App waits/retries or shows database locked error gracefully. | N/A |
| **TC-T2-24** | DB - Corruption Recovery | DB | Attempt start or save when DB is corrupt. | Overwrite `%APPDATA%\LicenseIssuerCSharp\licenses.db` with junk bytes, open app | SQLite error is caught and app recreates the database schema. | N/A |
| **TC-T2-25** | Fingerprint - Unicode Characters | CLI | Attempt generating token for a machine with Unicode/Arabic characters. | `--issue --machine "أجهزة-المتجر"` | Output error: Machine fingerprint contains invalid characters, or normalizes it. | Non-zero |
| **TC-T2-26** | Clock - System Rollback | CLI | Verify token validation if system clock is rolled back. | Set system clock back 1 hour, then attempt token verification | Error: System clock rollback detected. | Non-zero |
| **TC-T2-27** | Clock - Future iat | CLI | Verify token validation if issue time is in the future. | Token `iat` set to +2 hours, attempt token verification | Error: Token issue time is in the future. | Non-zero |

---

### Tier 3: Cross-Feature Combinations (6 Test Cases)

- **TC-T3-01: End-to-End Keypair Generation and CLI Sign-Verify Flow**
  - **Description**: Verifies that a keypair generated by the CLI `--generate-keys` command can be used to issue a new token with `--issue`, and then verified using `--verify` with the new public key.
  - **Inputs/CLI flags**:
    1. `LicenseIssuerCSharp.exe --generate-keys --out "d:\temp_keys"`
    2. `LicenseIssuerCSharp.exe --issue --machine "3A4B5C6D" --days 30 --key "d:\temp_keys\private_key.pem"`
    3. `LicenseIssuerCSharp.exe --verify --token "<TOKEN_FROM_STEP_2>" --machine "3A4B5C6D" --pubkey "d:\temp_keys\public_key.pem"`
  - **Expected Output Pattern**:
    - Step 1: Successful file creation message.
    - Step 2: Valid token printed to stdout.
    - Step 3: Success message indicating token is valid.
  - **Expected Exit Code**: 0 for all steps.

- **TC-T3-02: GUI Issue, DB Query, and CLI Verify Integration**
  - **Description**: Verifies integration between the GUI license issuer, SQLite storage, and the CLI verifier.
  - **Inputs/CLI flags**:
    1. In GUI, issue license for machine `"B8C9D0"` for 45 days.
    2. Extract the saved token from the database table `licenses` using SQL query.
    3. Execute CLI: `LicenseIssuerCSharp.exe --verify --token "<EXTRACTED_TOKEN>" --machine "B8C9D0"`
  - **Expected Output Pattern**:
    - GUI: Token displayed in text box.
    - DB: Row returned matching fingerprint and token.
    - CLI: Output contains verification success message.
  - **Expected Exit Code**: 0 (for CLI verify).

- **TC-T3-03: Settings Key-Change and Sign-Verify Cross-Check**
  - **Description**: Verifies that custom private key settings configured in the GUI settings tab correctly propagate to the GUI license signing engine.
  - **Inputs/CLI flags**:
    1. GUI Settings -> Generate new keypair in `"d:\custom_keys"`.
    2. GUI Settings -> Browse and load `"d:\custom_keys\private_key.pem"`.
    3. GUI Issue License -> Issue a license for machine `"1A2B3C"` for 60 days.
    4. Run CLI: `LicenseIssuerCSharp.exe --verify --token "<GUI_TOKEN>" --machine "1A2B3C"` (uses default public key).
    5. Run CLI: `LicenseIssuerCSharp.exe --verify --token "<GUI_TOKEN>" --machine "1A2B3C" --pubkey "d:\custom_keys\public_key.pem"`.
  - **Expected Output Pattern**:
    - Step 4: Verification failure (signature invalid since default key was not used).
    - Step 5: Verification success (signature valid since custom public key matches the loaded private key).
  - **Expected Exit Code**: Step 4 exit code = 1, Step 5 exit code = 0.

- **TC-T3-04: Metrics Synchronization between CLI and Dashboard**
  - **Description**: Verifies that licenses issued via CLI mode are correctly accounted for and synchronized in the GUI Dashboard metrics card display.
  - **Inputs/CLI flags**:
    1. Initial check of Dashboard counts (e.g. Total=X, Active=Y, Expired=Z).
    2. CLI issue command: `LicenseIssuerCSharp.exe --issue --machine "CLI_USER_1" --days 10`.
    3. Launch GUI Dashboard tab.
    4. View metrics cards.
  - **Expected Output Pattern**: Dashboard cards display Total = X+1, Active = Y+1, Expired = Z.
  - **Expected Exit Code**: 0.

- **TC-T3-05: Multi-Instance Database Lock and Concurrent Access**
  - **Description**: Verifies that multiple running instances of the application can concurrently access the same SQLite database file for reading history and dashboard metrics without deadlocking.
  - **Inputs/CLI flags**:
    1. Launch GUI Instance A and GUI Instance B.
    2. In Instance A, issue a new license for `"CLIENT_A"`.
    3. In Instance B, navigate to History tab and click refresh.
  - **Expected Output Pattern**: Instance B displays the new license record issued by Instance A. No file lock exceptions are raised.
  - **Expected Exit Code**: N/A (UI state).

- **TC-T3-06: Verification Engine Integration with Store App**
  - **Description**: Verifies that a token issued via the CLI can be imported and successfully verified by the main GroceryStoreManagement application's LicenseService.
  - **Inputs/CLI flags**:
    1. Generate token using `LicenseIssuerCSharp.exe --issue --machine "<FINGERPRINT>" --days 30`.
    2. Write the token to the database file of `GroceryStoreManagement` or enter it via the Store App's LicenseActivationWindow.
    3. Launch GroceryStoreManagement.
  - **Expected Output Pattern**: The GroceryStoreManagement app starts successfully in active state (no activation dialog shown).
  - **Expected Exit Code**: 0.

---

### Tier 4: Real-World Application Scenarios (6 Test Cases)

- **TC-T4-01: End-to-End Client License Delivery Workflow**
  - **Description**: Simulates the full workflow of an administrator receiving a customer fingerprint, processing it (stripping whitespace, lower to uppercase normalization), generating a 365-day license using the default embedded key, verifying it, and saving it to history.
  - **Inputs/CLI flags**:
    - Machine Fingerprint input: `"  e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855  "`
    - Client Name input: `"Amine Grocery Store"`
    - Action: `LicenseIssuerCSharp.exe --issue --machine "  e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855  " --days 365 --issuer "StoreOwner"`
    - Verify Action: `LicenseIssuerCSharp.exe --verify --token "<TOKEN>" --machine "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855"`
  - **Expected Output Pattern**:
    - Issue outputs a token payload string.
    - Base64Url decoding the token payload segment yields JSON:
      `{"machine":"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", "exp": "...", "iat": "...", "issuer": "StoreOwner", "nonce": "..."}`.
    - Verify outputs a success confirmation.
  - **Expected Exit Code**: 0.

- **TC-T4-02: Key Compromise and Migration Workflow**
  - **Description**: Simulates a scenario where the default embedded key is compromised. The admin generates a new 3072-bit key pair, configures the issuer with the new private key, issues a license, and confirms verification fails with the old key but succeeds with the new key.
  - **Inputs/CLI flags**:
    1. Generate new keys: `LicenseIssuerCSharp.exe --generate-keys --out "d:\secure_keys"`
    2. Issue license with new private key: `LicenseIssuerCSharp.exe --issue --machine "SECURE_FINGERPRINT" --days 30 --key "d:\secure_keys\private_key.pem"`
    3. Verify with old default public key: `LicenseIssuerCSharp.exe --verify --token "<NEW_TOKEN>" --machine "SECURE_FINGERPRINT" --pubkey "GroceryStoreManagement/Security/default_license_public_key.pem"`
    4. Verify with new public key: `LicenseIssuerCSharp.exe --verify --token "<NEW_TOKEN>" --machine "SECURE_FINGERPRINT" --pubkey "d:\secure_keys\public_key.pem"`
  - **Expected Output Pattern**:
    - Step 3 output contains: `"التوقيع الرقمي للكود غير صحيح."` (digital signature invalid).
    - Step 4 output contains verification success.
  - **Expected Exit Code**: Step 3 exit code = 1, Step 4 exit code = 0.

- **TC-T4-03: Trial License Expiry and Renewal Workflow**
  - **Description**: Simulates a customer trial sequence. An admin issues a trial license for 7 days, which expires. The admin then issues a renewal license for 30 days. The database history and verifier must correctly reflect the transition.
  - **Inputs/CLI flags**:
    1. Issue trial: `LicenseIssuerCSharp.exe --issue --machine "TRIAL_USER" --days 7`
    2. Save trial token to store application SQLite database.
    3. Simulate 7 days passage (by updating database `ExpiresAtUtc` column in `LicenseState` table to past date).
    4. Run store application and verify it detects expiration (exits or prompts for license with message `"انتهت مدة الترخيص"`).
    5. Issue renewal: `LicenseIssuerCSharp.exe --issue --machine "TRIAL_USER" --days 30`
    6. Save renewal token to store database and verify store app starts successfully in Active state.
  - **Expected Output Pattern**:
    - Step 4 shows expiration state (LicenseState = Expired).
    - Step 6 shows active state (LicenseState = Active).
  - **Expected Exit Code**: 0.

- **TC-T4-04: Store App Deployment and Instant Activation Workflow**
  - **Description**: Simulates deployment of a new store management instance. Since the store application embeds the public key, the license is issued via default fallback private key and verified immediately without any key configuration files.
  - **Inputs/CLI flags**:
    1. Obtain client fingerprint: `"9E8D7C6B5A4F3E2D"`
    2. Generate token using `LicenseIssuerCSharp.exe --issue --machine "9E8D7C6B5A4F3E2D" --days 365` (uses default private key).
    3. Activate token in GroceryStoreManagement (reads embedded public key `default_license_public_key.pem`).
  - **Expected Output Pattern**: Activation succeeds, database is updated, and store app runs.
  - **Expected Exit Code**: 0.

- **TC-T4-05: Multi-client Rollout and Analytics Audit Workflow**
  - **Description**: Simulates deploying to 10 machines. The admin generates 10 licenses (5 active, 5 expired). The issuer's history and dashboard metrics are audited to ensure accurate counting.
  - **Inputs/CLI flags**:
    1. Issue 5 licenses with `--days 30` for machines `"M1"` to `"M5"`.
    2. Issue 5 licenses with `--days -1` (expired) for machines `"M6"` to `"M10"`.
    3. Open the WPF application Dashboard.
  - **Expected Output Pattern**:
    - Dashboard displays Total = 10, Active = 5, Expired = 5.
    - History displays a table containing all 10 entries with their respective expiration statuses.
  - **Expected Exit Code**: N/A (UI state).

- **TC-T4-06: Hostile Clock Tampering Lockout Workflow**
  - **Description**: Simulates an adversarial user attempting to bypass license expiration by setting their system clock backward. The store management application must detect the rollback and lock the system. The admin then issues a new license to unlock.
  - **Inputs/CLI flags**:
    1. Issue a valid token for machine `"TAMPER_MACHINE"` for 30 days.
    2. Activate the license in the store application (database records `LastValidatedUtc = DateTime.UtcNow`).
    3. Move system clock back by 30 minutes.
    4. Start the store application.
    5. Admin issues a new license key via `LicenseIssuerCSharp.exe --issue --machine "TAMPER_MACHINE" --days 30`.
    6. Input new license key in store activation dialog.
  - **Expected Output Pattern**:
    - Step 4: Verification fails with message `"تم قفل النظام بسبب تغيير ساعة الجهاز للخلف."` and database `IsLocked` is set to 1.
    - Step 6: System successfully unlocks, resets `IsLocked` to 0, and transitions to Active.
  - **Expected Exit Code**: 0.
