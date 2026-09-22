# Real Keycloak witness

These realms are disposable test fixtures. The published passwords and TOTP seed are test data, never production identities. Both clients require authorization code with S256 PKCE; password grants are disabled. The Approvers realm requires password and TOTP. Access-token lifetime is 900 seconds and realm session lifetime is eight hours.

Run tools/Invoke-KeycloakWitness.ps1 with paths to a disposable PEM certificate and key (IP SAN 127.0.0.1). Requirements: .NET SDK from global.json, PostgreSQL test binaries, and a working local Docker Linux engine. The script builds with KeycloakWitness=true, starts the container bound only to loopback, runs the Witness=KeycloakContainer test, and removes its own container. Production OIDC code does not accept a self-signed certificate; only the test backchannel pins the supplied certificate.

The explicit witness build compiles KeycloakContainerTests.cs. The test fails if its local URL or certificate pin is absent; it does not silently skip. Ordinary builds exclude this external-runtime witness. For an already running isolated container, set SESS_KEYCLOAK_WITNESS_URL and SESS_KEYCLOAK_CERT_SHA256, build with -p:KeycloakWitness=true, then run dotnet test with --no-build --filter Witness=KeycloakContainer.

The test performs real browser-form authorization-code/PKCE login, including a TOTP challenge for Approvers. It starts a disposable PostgreSQL cluster, applies migrations, provisions and verifies restricted database principals, and serves the production authentication/identity middleware and session endpoint. It checks normal access, wrong-pool refusal, missing/unmapped company refusal, unmapped and revoked identity refusal, ID-token refusal and signature-tampering refusal. It changes provider endpoints and claim configuration; there is no Keycloak branch in application code.

Base image used on this PC: quay.io/keycloak/keycloak:26.7.3
Resolved base image digest: quay.io/keycloak/keycloak@sha256:ff4257d0d64efbe99ed1ddfaf07765cc3c36dc7518bf8324d41961327f441c54

The Windows PC has no installed Docker engine. For this session, a checksum-verified portable QEMU VM runs Alpine Linux and Docker. Only the disposable guest disk was formatted; the owner ERP database is not used. Initial image startup under CPU emulation takes substantially longer than native Docker. The actual authentication result belongs in the Item 16 evidence report; these fixture instructions are not a pass claim.

The successful API witness on this PC used a fixture-configured image committed after stock Keycloak augmentation and offline realm import. The exact derived image and container IDs, along with the passing result, are recorded in docs/installation/item-16-verification.md. A fresh invocation of the wrapper imports the same checked-in realms into the pinned-version base image.

## PostgreSQL backup/recovery witness

KeycloakWitness also compiles the separate Witness=KeycloakRecovery test. It is
not selected by the existing login-only wrapper. Run it explicitly after a
successful witness-enabled build, with a TRX logger and a unique results folder.

Set SESS_KEYCLOAK_RECOVERY_CONTROL to an absolute path to the disposable
controller script. tools/Control-KeycloakRecoveryWitness.ps1 is the Docker
adapter; it also needs SESS_KEYCLOAK_WITNESS_CERTIFICATE and
SESS_KEYCLOAK_WITNESS_PRIVATE_KEY. The existing URL and SHA256 pin variables still
apply. Reserve the loopback HTTPS port for this witness. The controller starts
only a labelled disposable provider using the test's fresh PostgreSQL port;
never point it at customer data. The published database/password/user fixtures
are test data and must never be deployed.

The test uses the real scheduled Installer backup, recovers to another durable
PostgreSQL cluster, makes its original identity database unavailable, and verifies
fresh password/TOTP login, unchanged subjects/signing keys, and ERP OIDC/mapping
behaviour. It also verifies recovery of a deployment-configuration file. The
protected TLS mount is fixture infrastructure, not a witnessed production TLS
installation. Routine builds exclude this external-runtime test.

On this Windows PC the actual container runs in the retained, isolated QEMU
Linux guest via a local control adapter; the generic Docker adapter has only been
syntax-checked here. The provider is production-mode/PostgreSQL-backed, but the
VM's CPU emulation and native Windows test PostgreSQL are not a production Linux
appliance certification. Initial emulated startup may take up to 40 minutes.
The local adapter retains the augmented server image between restarts; user and
realm data remain in PostgreSQL. See item-16-identity-recovery-findings.md and
item-16-local-keycloak-operations.md under docs/installation for evidence and
site deployment boundaries.