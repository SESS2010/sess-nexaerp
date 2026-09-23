# Office PC HTTPS trust — all eleven PCs

Issued 22 September 2026. Administrator performs this once on each office PC.
Receive ONLY **SESS-Office-Root.cer** from the approved maintainer. Never copy the
ERP PFX, Keycloak private key or root private PFX to an office PC.

1. In PowerShell, run `certutil -hashfile .\SESS-Office-Root.cer SHA256`.
   Compare the ENTIRE value with the maintainer's separately supplied record:

   **A95109800BD04600670FAD6E4316996FEB5B153D03EA07493ED8BE691F796913**

   Stop if any character differs. The approved subject is CN=SESS Office Root CA.
2. Double-click the .cer, choose **Install Certificate**, **Local Machine**,
   approve the administrator prompt. Choose **Place all certificates in the
   following store**, then **Trusted Root Certification Authorities**. Finish.
3. Close and reopen Microsoft Edge or Chrome. Once the services are commissioned,
   open **https://192.168.68.130:8443** and
   **https://192.168.68.130:8444/realms/staff/account**. Both must have trusted
   HTTPS, with no certificate warning. Do not click through a warning.
   An ERP deployment gate before the frontend is ready is separate from TLS.
4. If warned, stop and give the maintainer the exact address and warning.
   They check the clock, root trust, certificate SAN, chain and dates.
5. Record PC hostname, date, browser, both HTTPS results and the checker below.
   The maintainer collects eleven completed checks. No skipped certificate validation.

PC hostname: __________________  Date: __________  Browser: ______________

ERP 8443 trusted: __________  Keycloak 8444 trusted: __________

Checked by: __________________  Exception/follow-up: ______________________

Server leaf certificates expire 22 September 2027; renew by 23 August 2027.
This root is public. Its private key is never needed to trust an office PC.
