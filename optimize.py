#!/usr/bin/env python3
"""
YTD Optimizer Wrapper - Calls the CodeWalker-based YtdOptimizer
"""

import subprocess
import sys
import os
from pathlib import Path

def main():
    script_dir = Path(__file__).parent.absolute()
    
    # Find YtdOptimizer.exe
    ytd_optimizer = script_dir / "tools" / "YtdOptimizer" / "bin" / "Release" / "net7.0" / "YtdOptimizer.exe"
    dotnet_exe = Path(os.environ.get("USERPROFILE", "")) / "AppData" / "Local" / "Microsoft" / "dotnet" / "dotnet.exe"
    
    if len(sys.argv) < 3:
        print("YTD Optimizer - Optimizes GTA V / FiveM texture files")
        print("Usage: python optimize.py <input_folder> <output_folder> [max_size]")
        print("Example: python optimize.py input output 512")
        return
    
    input_folder = sys.argv[1]
    output_folder = sys.argv[2]
    max_size = sys.argv[3] if len(sys.argv) > 3 else "512"
    texconv_path = str(script_dir / "tools" / "texconv.exe")
    
    # Check if we can run the exe directly or need dotnet
    if ytd_optimizer.exists():
        cmd = [str(ytd_optimizer), input_folder, output_folder, max_size, texconv_path]
    elif dotnet_exe.exists():
        csproj = script_dir / "tools" / "YtdOptimizer" / "YtdOptimizer.csproj"
        cmd = [str(dotnet_exe), "run", "--project", str(csproj), "--", 
               input_folder, output_folder, max_size, texconv_path]
    else:
        print("Error: Neither YtdOptimizer.exe nor dotnet.exe found")
        print("Run 'dotnet build -c Release' in tools/YtdOptimizer first")
        return
    
    subprocess.run(cmd)

if __name__ == "__main__":
    main()
