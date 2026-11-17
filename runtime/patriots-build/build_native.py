"""Windows x64 build. Requires Python 3, cmake, Ninja, unicorn==2.1.4 and LLVM-MinGW."""
from pathlib import Path
import argparse,subprocess,sys,shutil
p=argparse.ArgumentParser();p.add_argument('--toolchain',type=Path,required=True);p.add_argument('--ninja',default='ninja');p.add_argument('--out',type=Path,default=Path('patriots-built'));a=p.parse_args()
try:import unicorn
except ImportError:sys.exit('Install dependency: py -3 -m pip install unicorn==2.1.4')
root=Path(__file__).resolve().parent.parent; support=root/'patriots-build'; build=root/'patriots-build-output';build.mkdir(exist_ok=True)
tool=a.toolchain.resolve(); u=Path(unicorn.__file__).parent; ninja=shutil.which(a.ninja) or str(Path(a.ninja).resolve())
cross=build/'toolchain.cmake';cross.write_text('\n'.join(['set(CMAKE_SYSTEM_NAME Windows)','set(CMAKE_SYSTEM_PROCESSOR AMD64)']+[f'set({var} "{(tool/"bin"/exe).as_posix()}")' for var,exe in [('CMAKE_C_COMPILER','x86_64-w64-mingw32-clang.exe'),('CMAKE_CXX_COMPILER','x86_64-w64-mingw32-clang++.exe'),('CMAKE_RC_COMPILER','x86_64-w64-mingw32-windres.exe')]]),encoding='utf-8')
subprocess.run(['cmake','-S',str(root/'mgs_native'),'-B',str(build),'-G','Ninja','-DCMAKE_TOOLCHAIN_FILE='+str(cross),'-DCMAKE_MAKE_PROGRAM='+ninja,'-DUNICORN_ROOT='+str(u),'-DUNICORN_LIB='+str(support/'libunicorn.dll.a'),'-DMGS_OUTPUT_DIR='+str(a.out.resolve()),'-DCMAKE_BUILD_TYPE=Release'],check=True)
subprocess.run(['cmake','--build',str(build),'--parallel','4'],check=True)
print('Built:',a.out.resolve()/'mgs_mobile.exe')
