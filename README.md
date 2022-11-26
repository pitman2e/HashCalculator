# Hash Calculator
Homemade simple hash calculator to check for any 'Bit Rot' (For filesystem without file integrity detection)
# About 'Bit Rot'
https://en.wikipedia.org/wiki/Data_degradation

# Usage
```
  -d, --directory    Required. Root directory to scan
  -i, --interval     (Default: 365) Days before rescan
  -t, --threshold    (Default: 20) Scan threshold (Gigabyte)
  -v, --verbose      Print verbose message
  -n, --new          Only Hash folder without hash file
  --help             Display this help screen.
  --version          Display version information.
```

# Example Usage
Regular scan should call this (20GB Scan Threshold)
```
./HashCalculator -v -d "/mnt/F/My Pictures/"
```

Scan until interrupted (Sca Threshold = 0)
```
./HashCalculator -v -t 0 -d "/mnt/F/My Pictures/"
```

# TODO
- Count file count