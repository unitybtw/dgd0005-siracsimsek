"""Opt-in, repeatable local traffic experiment using actual Development-build clients."""
from pathlib import Path
import subprocess,time,json,statistics
root=Path(__file__).resolve().parents[1];out=root/'verification/benchmarks';out.mkdir(parents=True,exist_ok=True)
binary='/private/tmp/PongQA.app/Contents/MacOS/Pong'
summary={}
for rate in [10,60,30]:
 print(f'Starting {rate} Hz: one listen-server host and one guest, automatic owner paddle tracking.',flush=True)
 processes=[]
 try:
  for role in ['host','guest']:
   probe=out/f'{rate}-{role}.jsonl';probe.write_text('')
   args=[binary,'-batchmode','-nographics','-pongClient','-pongBenchmark','-pongTickRate',str(rate),'-pongProbe',str(probe),'-logFile',str(out/f'{rate}-{role}.log')]
   if role=='host':args+=['-pongServer']
   processes.append(subprocess.Popen(args,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL))
   if role=='host':time.sleep(2)
  time.sleep(32)
 finally:
  for p in processes:p.terminate()
  for p in processes:
   try:p.wait(timeout=10)
   except subprocess.TimeoutExpired:p.kill();p.wait()
 host=[json.loads(x) for x in (out/f'{rate}-host.jsonl').read_text().splitlines()]
 run=[];eligible=[]
 for sample in host:
  if sample['inPlay']:
   run.append(sample)
   if sample['time']-run[0]['time']>=10:eligible.append(sample)
  else:run=[]
 if not eligible:raise RuntimeError(f'{rate} Hz had no continuous rally lasting ten seconds')
 values=[s['serverOut'] for s in eligible]
 summary[str(rate)]={'server_out_Bps_after_10s':round(values[0],1),'mean_server_out_Bps_after_10s':round(statistics.mean(values),1),'samples':len(values),'clients':2,'auto_tracking':True,'last_score':[host[-1]['left'],host[-1]['right']]}
 print(json.dumps({rate:summary[str(rate)]}),flush=True)
 (out/'summary.json').write_text(json.dumps(summary,indent=2)+'\n')
