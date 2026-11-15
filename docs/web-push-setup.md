# Employee Web Push

Push is disabled by default. Generate one persistent P-256 VAPID key pair outside this repository with the WebPush library's `VapidHelper.GenerateVapidKeys()` in a temporary console tool. Store the private key in your deployment secret store. Never commit keys, endpoints, or device encryption secrets. Keep the same pair across restarts; rotating it requires devices to unsubscribe and enable again.

Inject `Push__Enabled=true`, `Push__VapidPublicKey`, `Push__VapidPrivateKey`, and `Push__VapidSubject` into the deployed API process/container. The subject must be your verified operator `mailto:` address or HTTPS URL. The `.env` example lists names only: Docker Compose must explicitly pass these variables to the API environment or use your platform's secret injection. Invalid enabled configuration stops startup without printing secrets. Disabled configuration permits normal application startup and device deletion.

Use HTTPS. iPhone/iPad require iOS/iPadOS 16.4 or later and GF installed with Add to Home Screen. Enable notifications explicitly in employee Profile and allow OS permission. Each device opts in separately. Logout and Disable unsubscribe that device. Denied permission must be changed in browser/system settings.

Verify `/sw.js` returns JavaScript with revalidation headers, rather than the SPA HTML fallback. The worker has no offline cache. Existing manifest identity and deploy cache rules stay unchanged.

The server reconciles the four current Inbox classes every 30 seconds, including future availability windows. It excludes old items predating device opt-in and server-read items. Persistent delivery records and notification tags prevent routine duplicates; crash retries can still duplicate. Transient offers disappearing between scans may produce no push. Gateway acceptance does not guarantee OS display. Real-device verification requires a push-enabled HTTPS deployment; mocks do not prove iOS delivery.

The gateway host allowlist rejects arbitrary endpoints and redirects. Extend `Push__AllowedEndpointHosts__N` only for verified browser vendor service hosts. Never allow internal hosts or IP addresses.

Gateway TTL is capped to the item's remaining five-day retention, with a one-hour maximum for open offers. Jobs use two-minute leases, a 100-send scan cap, eight attempts maximum, and exponential retry delays of 60 seconds through one hour. Finalized journal entries are removed after five days. No database transaction spans a gateway request.
