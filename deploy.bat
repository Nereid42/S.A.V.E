
@echo off

rem This is the deploy script for the LGG project file

rem H is the destination game folder
rem GAMEDIR is the name of the mod folder (usually the mod name)
rem GAMEDATA is the name of the local GameData
rem VERSIONFILE is the name of the version file, usually the same as GAMEDATA,
rem    but not always

set H=%KSPDIR%
set GAMEDIR=Nereid\S.A.V.E.
set GAMEDATA="GameData"
set VERSIONFILE=%GAMEDIR%.version

mkdir "%GAMEDATA%\%GAMEDIR%\Plugins"

copy /Y "%1%2" "%GAMEDATA%\%GAMEDIR%\Plugins"

xcopy /y /s /I %GAMEDATA%\%GAMEDIR% "%H%\GameData\%GAMEDIR%"

pause
