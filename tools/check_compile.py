"""Compile against installed Unity assemblies; does not replace Unity import/RPC weaving."""
from pathlib import Path
import subprocess
root=Path(__file__).resolve().parents[1]
unity=Path('/Applications/Unity/Hub/Editor/6000.3.2f1/Unity.app/Contents/Resources/Scripting')
tmp=Path('/private/tmp/pong-compile');tmp.mkdir(exist_ok=True)
refs=list((unity/'NetStandard/ref/2.1.0').glob('*.dll'))
refs+=list((unity/'Managed/UnityEngine').glob('*.dll'))
refs+=[unity/'Managed/Newtonsoft.Json.dll']
refs+=list((root/'Pong/Assets/FishNet/Runtime/Plugins/CodeAnalysis').glob('FishNet.CodeAnalysis.dll'))
cache=unity.parent/'PackageManager/ProjectTemplates/libcache/com.unity.template.3d-cross-platform-17.0.14/ScriptAssemblies'
refs+=list(cache.glob('*.dll'))
refs=[p for p in refs if p.name not in ['Assembly-CSharp.dll','Assembly-CSharp-Editor.dll'] and not (p.parent.name=='unity-4.8-api' and p.name.startswith('Unity'))]
# Keep one reference for each assembly filename.
refs=list({p.name:p for p in refs}.values())
common=['-nologo','-target:library','-unsafe','-langversion:latest','-nostdlib+','-define:UNITY_2018_3_OR_NEWER,UNITY_EDITOR,UNITY_EDITOR_OSX,UNITY_STANDALONE_OSX,UNITY_6000_0_OR_NEWER,UNITY_2022_3_OR_NEWER,UNITY_2022_2_OR_NEWER,UNITY_2021_3_OR_NEWER,UNITY_2021_2_OR_NEWER,UNITY_2020_3_OR_NEWER']+['-r:'+str(p) for p in refs]
def compile(name,sources,extra=[], editor=True):
 rsp=tmp/(name+'.rsp');out=tmp/(name+'.dll')
 args=common if editor else [a for a in common if not a.startswith('-define:')]
 rsp.write_text('\n'.join('"'+arg+'"' for arg in args+['-out:'+str(out)]+extra+[str(s) for s in sources]))
 result=subprocess.run([str(unity/'NetCoreRuntime/dotnet'),str(unity/'DotNetSdkRoslyn/csc.dll'),'@'+str(rsp)],text=True,capture_output=True)
 (root/'verification'/(name+'.log')).write_text(result.stdout+result.stderr)
 print(name, 'PASS' if result.returncode==0 else 'FAIL'); print('\n'.join(line for line in (result.stdout+result.stderr).splitlines() if 'error CS' in line))
 return result.returncode,out
code,out=compile('FishNetStatic',list((root/'Pong/Assets/FishNet/Runtime').rglob('*.cs')))
if code:raise SystemExit(code)
sources=list((root/'Pong/Assets/Scripts').rglob('*.cs'))+list((root/'Pong/Assets/Editor').rglob('*.cs'))
# The MCP editor helper is covered by actual Unity import/build logs, not this standalone API check.
sources=[p for p in sources if p.name!='PongEditorTools.cs']
code,_=compile('PongStatic',sources,['-r:'+str(out)])
raise SystemExit(code)
