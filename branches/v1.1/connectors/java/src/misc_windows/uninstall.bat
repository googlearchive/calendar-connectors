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

rem Does the service executable exist?
rem ==================================
:CONFIGEXISTS
if exist GoogleCalendarConnector_plugin.exe goto SERVICEEXISTS
echo GoogleCalendarConnector_plugin.exe not found!
goto EXIT

rem Uninstall the current service
rem =============================
:SERVICEEXISTS
echo UNINSTALLING SERVICE...
net stop GoogleCalendarConnectorPlugIn
GoogleCalendarConnector_plugin.exe uninstall
if not errorlevel 1 goto UNINSTALLWORKED
echo ... DONE
echo COULD NOT UNINSTALL SERVICE!!!
echo PLEASE READ THE SCREEN OUTPUT FOR FURTHER INFORMATION
goto EXIT

:UNINSTALLWORKED
rem Done :-)
rem ========
echo ...DONE
echo *
echo **********************
echo * Uninstall complete *
echo **********************
echo * 
:EXIT
pause
