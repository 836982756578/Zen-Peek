@echo off
chcp 65001 >nul
echo ======================================================================
echo ☯️ ZenSuite Otomatik Derleme, Git Commit ve GitHub Release Guncelleyici
echo ======================================================================
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -Command "python ..\publisher.py (Split-Path -Leaf (Get-Location)) 'Guncelleme yapildi'"
echo.
echo Islem tamamlandi. Pencereyi kapatmak icin herhangi bir tusa basin...
pause >nul
