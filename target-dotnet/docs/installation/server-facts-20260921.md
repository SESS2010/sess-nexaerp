# Server facts accepted at 16:01, 21 September 2026

> **Protected server rule (21 September, 16:01):** NEVER stop, disable, modify or remove
> SESS_SQLEXPRESS, TEW_SQLEXPRESS, SQLBrowser, their ~77 SOLIDWORKS project databases,
> ewserver, ANY NI or Siemens service, ANY Rockwell FactoryTalk service, IIS Default
> Web Site or /Updater. Leave Wamp stopped/manual. Windows 10 stays; NEVER a clean
> Windows install; NEVER install a .NET SDK on this server. All application here is
> by the server agent, not from the laptop. See C:\SESS-ServerPrep for completed preparation.

This supersedes earlier laptop/VM assumptions and the morning resource measurement.
Completed preparation is recorded at **C:\SESS-ServerPrep**: time synchronization and
India Standard Time; high performance/no sleep/hibernation/device power-off; Guest
blocked; password-protected Admin untouched; Defender current with four-hour updates;
Vite 5173 stopped; RDP off; network Private. Do not repeat those actions.

DESKTOP-SPF5420: Windows 10 Pro 64-bit, Intel i5-10600K desktop CPU (six physical
cores, 12 logical; reported not throttled), 15.9 GB total RAM, **6.4 GB free**;
C: SSD **41.6 GB free**. Keep at least 25 GB free. D:/E: are the same suspect HDD;
do not put new backups or verification work on either. See the daily off-machine
backup procedure before enabling users; cable replacement alone does not certify it.

Physical disks remain C: WDC WDS240G2G0A 224 GB SSD, and D:/E: Seagate ST1000DM010
932 GB HDD. D:/E: are two partitions of the same failing/suspect disk, not two copies.
UPS is connected. Realtek Gaming GbE adapter currently links at 100 Mbps: adequate for
11 transactional users, with longer backup transfers. Address 192.168.68.130,
gateway 192.168.68.1; confirm router reservation. ASP.NET Core/.NET 10.0.12 runtime is
installed, SDK removed. Preserve those facts when recording later measurements.

PostgreSQL 17.11 runs as NetworkService; data is
C:\Program Files\PostgreSQL\17\data. Current listen_addresses='*', max_connections=100,
shared_buffers=128MB. Preserve **local-only pg_hba** and add **no 5432 firewall rule**.
Both API and Keycloak use 127.0.0.1. Existing wildcard binding is not LAN permission;
verify local-only pg_hba rather than opening access for the laptop.

Initial proposed tuning (server agent, PostgreSQL only, record/restart/check):
shared_buffers=512MB, effective_cache_size=2GB, work_mem=4MB,
maintenance_work_mem=128MB, max_connections=40, max_parallel_workers=2,
max_parallel_workers_per_gather=1. API Maximum Pool Size=15, Minimum Pool Size=0;
Keycloak pool min=1/initial=1/max=10 and Java heap -Xms128m -Xmx512m. Retain capacity
for admin/backup connections. Budget roughly 1 GiB for the API working set initially, then measure eleven-user
load and large-report peaks; this is a capacity allowance, not an enforced process
limit. Heap is not total JVM memory. Watch actual process
working sets and available RAM under eleven-user and engineering load; alert below
2 GB. Run restore verification serially outside peak load. Do not cap or stop protected
services to meet this budget. API GC configuration needs measured adjustment, not a
claimed hard total-memory limit. No .NET SDK installation is permitted.

Ports NEVER take: 80 IIS Default Web Site + Rockwell /Updater; 81 Siemens UMC;
102 Siemens S7; 7070 AnyDesk; 8080 NI ApplicationWebServer; 8082 Rockwell diagnostics;
8888 Siemens TIA Administrator; 26666 SOLIDWORKS Electrical; 27000 FlexNet;
50053/51771 SQL Server. 443 currently has a firewall rule but no listener.
IIS URL Rewrite is absent; ANCM V2 20.0.26234.12 is present. Neither is needed for
our service hosting. ERP remains **https://192.168.68.130:8443**. Reserve **8444** for
Keycloak HTTPS, subject to a fresh listener/service reservation check; if occupied,
STOP and report, never evict its owner. Keycloak management will bind loopback 9000,
also checked before installation. Do not use NI's 8080 or the ERP's 8443 for Keycloak.

Keep PostgreSQL data on C:. Storage capacity must now budget ERP + identity databases,
WAL, local verified dumps and one restore working area, not just the old 5-8 GB ERP
forecast. A six-month fit with the 25 GB floor is not established until those sizes
are measured; the backup procedure supplies the admission/retention gate.
