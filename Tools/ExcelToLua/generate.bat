@echo off
pushd "%~dp0\..\.."
python Tools\ExcelToLua\generator.py
if errorlevel 1 (
    echo.
    echo 生成失败，请确认已安装 Python 3 并已加入 PATH。
    pause
)
popd
