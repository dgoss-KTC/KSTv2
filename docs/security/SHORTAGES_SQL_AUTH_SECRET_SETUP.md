# Shortages SQL-Auth Secret Setup

**Status:** Stage 10.3C bundled-resource prerequisite — no database access

## Operator setup

Before creating a desktop release package, the release operator places the real, untracked file at:

```text
src\tauri\resources\secrets.json
```

`src/tauri/resources/secrets.example.json` is a tracked placeholder-only schema reference. The real
file is ignored by Git and must not be copied into application configuration, logs, support
artifacts, or any other tracked location. Tauri packaging copies the real file beside the installed
application and resolves that bundled resource at runtime. It supplies the Kst.Api sidecar only the
resource path in `KST_SECRETS_FILE`, never the secret contents.

Create the desktop release package only with this command from `src/tauri`:

```text
npx @tauri-apps/cli build --config tauri.release.conf.json
```

The release overlay declares `resources/secrets.json`, so the command fails before packaging if the
release operator has not supplied the untracked file. Base `tauri.conf.json` does not declare that
resource: ordinary development and tests run without it and leave Shortages disabled.

The example schema is:

```json
{
  "Shortages": {
    "Server": "KNWVM13",
    "Database": "keytronicshortage",
    "Username": "<dedicated read-only SQL login>",
    "Password": "<secret>",
    "Encrypt": true,
    "TrustServerCertificate": false
  }
}
```

`Encrypt` defaults to `true`; `TrustServerCertificate` defaults to `false`. Operators may override
either transport setting only after validating the real server transport. QAD transport settings do
not apply to this database.

Without `KST_SECRETS_FILE`, or with a missing, malformed, or incomplete file, the Shortages
integration remains disabled. Normal development does not create a file. KST does not fall back to
Windows-integrated authentication, blank credentials, or another database. The application does not
log, serialize, cache, or expose the secret configuration.

The installer places the bundled resource beside the desktop application. End users have no secret
file setup step.

## Next validation step

After owner/IT supplies the dedicated account through the bundled file, perform the bounded
Stage 10.3 source-validation preflight: verify TLS transport and effective identity, verify only
`CONNECT` plus required `SELECT` permissions, then conduct bounded metadata inspection and keyed
sample reads for `ShortageMaster` and `PreferredSuppliers`. Do not add readers or query those tables
until that evidence is accepted.
