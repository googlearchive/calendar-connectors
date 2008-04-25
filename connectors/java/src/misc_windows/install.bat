@echo off
rem Copyright (c) 2007 Google Inc.
rem
rem Licensed under the Apache License, Version 2.0 (the "License");
rem you may not use this file except in compliance with the License.
rem You may obtain a copy of the License at
rem
rem     http://www.apache.org/licenses/LICENSE-2.0
rem
rem Unless required by applicable law or agreed to in writing, software
rem distributed under the License is distributed on an "AS IS" BASIS,
rem WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
rem See the License for the specific language governing permissions and
rem limitations under the License.
rem 

rem Does the configuration file exist?
rem ==================================
if exist config.txt goto CONFIGEXISTS
echo config.txt not found!
goto EXIT

rem Does the service executable exist?
rem ==================================
:CONFIGEXISTS
if exist GoogleCalendarConnector_plugin.exe goto SERVICEEXISTS
echo GoogleCalendarConnector_plugin.exe not found!
goto EXIT

rem Does the self test exist?
rem =========================
:SERVICEEXISTS
if exist SelfTest.exe goto SELFTESTEXISTS
echo SelfTest.exe not found!
goto EXIT

rem Uninstall the current service
rem =============================
:SELFTESTEXISTS
echo UNINSTALLING SERVICE (IF NECESSARY)...
net stop GoogleCalendarConnectorPlugIn
GoogleCalendarConnector_plugin.exe uninstall
echo ... DONE

rem Running self test
rem =================
echo RUNNING SELF TEST...
SelfTest 1>selftest.log 2>&1
if not errorlevel 1 goto SELFTESTWORKED
echo ***Self-Test failed, please check selftest.log for more details***
echo INSTALLATION ABORTED
goto EXIT

rem Setup service
rem =============
:SELFTESTWORKED
echo ...DONE
echo INSTALLING SERVICE...
GoogleCalendarConnector_plugin.exe install
if not errorlevel 1 goto SETUPWORKED
echo INSTALLATION ABORTED
goto EXIT

:SETUPWORKED
rem Done :-)
rem ========
echo ...DONE
echo *
echo *************************
echo * Installation complete *
echo *************************
echo * 
:EXIT
pause
