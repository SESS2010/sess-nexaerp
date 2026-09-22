from pathlib import Path
import argparse, hashlib, json, os, shutil, socket, subprocess, uuid, datetime

parser=argparse.ArgumentParser()
parser.add_argument('--bundle',required=True)
parser.add_argument('--installer',required=True)
parser.add_argument('--evidence',required=True)
parser.add_argument('--pg-bin',default=r'C:\Program Files\PostgreSQL\17\bin')
args=parser.parse_args()
repo=Path(__file__).resolve().parents[2]
evidence=Path(args.evidence).resolve();evidence.mkdir(parents=True,exist_ok=True)
pg=Path(args.pg_bin).resolve();bundle=Path(args.bundle).resolve();installer=Path(args.installer).resolve()
# One witness owns one new cluster; never discover/connect to a field cluster.
cluster=evidence/('cluster-'+uuid.uuid4().hex);data=cluster/'data';cluster.mkdir()
with socket.socket() as sock:
 sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
assert port not in (5432,5433)
runtime=evidence/'runtime-only'
if not runtime.exists():
 runtime.mkdir();installed=Path(os.environ.get('ProgramFiles',r'C:\Program Files'))/'dotnet'
 shutil.copy2(installed/'dotnet.exe',runtime/'dotnet.exe')
 shutil.copytree(installed/'host',runtime/'host')
 for framework in ('Microsoft.NETCore.App','Microsoft.AspNetCore.App'):
  versions=[p for p in (installed/'shared'/framework).iterdir() if p.name.startswith('10.')]
  version=max(versions,key=lambda p:tuple(int(n) for n in p.name.split('.')))
  shutil.copytree(version,runtime/'shared'/framework/version.name)
assert not (runtime/'sdk').exists()
env=os.environ.copy()
for key in list(env):
 if key.startswith(('NEXAERP_','NexaErp__','ConnectionStrings__','Authentication__','DatabaseSecurity__')):
  env.pop(key)
env.update({'PATH':str(runtime)+';'+str(pg)+';'+os.environ['SystemRoot']+r'\System32',
 'DOTNET_ROOT':str(runtime),'DOTNET_ROOT_X64':str(runtime),'DOTNET_HOST_PATH':str(runtime/'dotnet.exe'),
 'DOTNET_MULTILEVEL_LOOKUP':'0','DOTNET_CLI_HOME':str(evidence/'cli-home')})
log=evidence/'witness.log'
def run(command, expected=0, timeout=600):
 with log.open('a',encoding='utf-8') as out:
  offset=out.tell()
  result=subprocess.run([str(x) for x in command],env=env,cwd=evidence,stdout=out,stderr=subprocess.STDOUT,text=True,timeout=timeout)
 with log.open('rb') as source:
  source.seek(offset);output=source.read().decode('utf-8')
 if result.returncode!=expected: raise RuntimeError(f'{Path(str(command[0])).name} failed: {result.returncode}; see {log}')
 return output
sdk=run([runtime/'dotnet.exe','--list-sdks']);assert not sdk.strip(),sdk
runtime_versions=run([runtime/'dotnet.exe','--list-runtimes'])
pgversion=run([pg/'psql.exe','--version']).strip()
def psql(db,text=None,file=None,owner=False):
 cmd=[pg/'psql.exe','-X','-w','-h','127.0.0.1','-p',str(port),'-U','nexa_erp_migration' if owner else 'postgres','-d',db,'-v','ON_ERROR_STOP=1','-At']
 if owner:cmd+=['-c','SET ROLE nexa_erp_owner']
 if text is not None:cmd+=['-c',text]
 if file is not None:cmd+=['-f',file]
 return run(cmd)
started=False
try:
 run([pg/'initdb.exe','-D',data,'-U','postgres','-A','trust','--encoding=UTF8','--no-locale','--no-sync'])
 run([pg/'pg_ctl.exe','-D',data,'-l',cluster/'postgres.log','-o',f'-h 127.0.0.1 -p {port}','-w','start']);started=True
 ids=sorted(p.stem for p in (repo/'src/SESS.NexaERP.Infrastructure/Persistence/Migrations').glob('20*.cs') if not p.name.endswith('.Designer.cs'))
 results=[]
 for db in ('sess_nexa_erp_DEMO','sess_nexa_erp'):
  run([pg/'createdb.exe','-h','127.0.0.1','-p',str(port),'-U','postgres',db])
  psql(db,'CREATE SCHEMA advance;')
  env['ConnectionStrings__NexaErpInstaller']=f'Host=127.0.0.1;Port={port};Database={db};Username=postgres;Pooling=false'
  env['NexaErp__ExpectedDatabase']=db
  for role in ('MIGRATION','BOOTSTRAP','RUNTIME'):env[f'NEXAERP_{role}_PASSWORD']='Witness-'+uuid.uuid4().hex+'-7!'
  run([installer,'database-principals','provision'])
  env['ConnectionStrings__NexaErp']=f'Host=127.0.0.1;Port={port};Database={db};Username=nexa_erp_migration;Options=-c role=nexa_erp_owner;Pooling=false'
  run([bundle])
  reconciled=run([installer,'database-principals','provision']);assert 'RECONCILED:' in reconciled
  verified=run([installer,'database-principals','status']);assert 'VERIFIED:' in verified
  history=psql(db,'SELECT "MigrationId" FROM advance."__EFMigrationsHistory" ORDER BY "MigrationId";').splitlines()
  assert history==ids, (len(history),len(ids))
  before=psql(db,'SELECT count(*) FROM advance.audit_logs;').strip()
  env['ConnectionStrings__NexaErp']=f'Host=127.0.0.1;Port={port};Database={db};Username=nexa_erp_migration;Options=-c role=nexa_erp_owner;Pooling=false'
  run([bundle])
  assert psql(db,'SELECT "MigrationId" FROM advance."__EFMigrationsHistory" ORDER BY "MigrationId";').splitlines()==history
  assert psql(db,'SELECT count(*) FROM advance.audit_logs;').strip()==before
  assert psql(db,'SELECT count(*) FROM advance.stock_movements;').strip()=='0'
  run([installer,'database-principals','provision']);run([installer,'database-principals','status'])
  results.append({'database':db,'migrations':len(history),'head':history[-1],'replay_history_and_audits_unchanged':True,'stock_movements':0,'reconciled':True,'verified':True})
  # Drop only this exact known disposable database, not any owner database.
  run([pg/'dropdb.exe','-h','127.0.0.1','-p',str(port),'-U','postgres',db])
 result={'completed_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'bundle_sha256':hashlib.sha256(bundle.read_bytes()).hexdigest(),
  'installer_sha256':hashlib.sha256(installer.read_bytes()).hexdigest(),
  'installer_assembly_sha256':hashlib.sha256(installer.with_suffix('.dll').read_bytes()).hexdigest(),
  'migration_sources':{str(p.relative_to(repo)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((repo/'src/SESS.NexaERP.Infrastructure/Persistence/Migrations').glob('*.cs'))},
  'sdk_list':sdk.strip(),'runtime_root':str(runtime),'runtime_versions':runtime_versions,'postgresql':pgversion,'loopback_port':port,'databases':results,
  'limitations':['SDK remains installed elsewhere on laptop, excluded from child PATH and runtime root; this is not an uninstalled-SDK OS image.',
   'Disposable trust-auth loopback cluster, not field password/TLS/network/locale/disk/performance conditions.',
   'Does not witness SCM reboot, SOLIDWORKS coexistence, remote browser, production frontend/OIDC or owner data.']}
 (evidence/'result.json').write_text(json.dumps(result,indent=2))
 print(json.dumps(result,indent=2))
finally:
 if data.exists():
  result=subprocess.run([str(pg/'pg_ctl.exe'),'-D',str(data),'-m','fast','-w','stop'],env=env,capture_output=True,text=True,timeout=120)
  if result.returncode!=0 and started: raise RuntimeError('Owned witness cluster cleanup failed: '+str(cluster))
