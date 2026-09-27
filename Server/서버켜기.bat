@echo off
chcp 65001 > nul
rem 산성의 패 대전 서버를 이 컴퓨터에서 켭니다 (Node.js 필요: https://nodejs.org)
cd /d "%~dp0"
if not exist node_modules call npm install
node server.js
pause
