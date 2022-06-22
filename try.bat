FOR /F "tokens=*" %%a in ('git rev-parse --short HEAD') do SET HASH=%%a
bz c builds\release-%HASH%.zip Assets .vscode
