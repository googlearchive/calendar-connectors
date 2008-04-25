===============================================
Google Calendar Connector Plugin - Source Files
===============================================

From a high level perspective, here is what needs to be done to build the sources:

0) Install JDK 6 or higher
==========================
Check http://java.sun.com for more details.

1) Install ANT
==============
You need a recent version of ANT installed (1.7.0 or higher). If you have ant pre-installed on your machine, you can enter "ant -version" to check the version number.
See http://ant.apache.org/manual/install.html for more details on how to successfully setup ANT.

2) Download dependencies
========================
This application depends on a couple external libraries and applications. Please read the MISSING.txt in
  lib
  antlib and
  testlib
and place the required files in those directories. Note that the files in antlib are optional, but you will have to delete the "JSmooth"-section from
the build.xml if you choose not to use them.

3) Try it out
=============
The following ant commands should be useful:

ant compile (same as just running ant) - will compile the sources and place a jar file in the dist subfolder
ant test - will compile and execute the unit tests
ant dist - will build the windows executables and package them with the install/uninstall batchfiles and a sample config.txt 
           (this OPTIONAL step will even work in a Linux build system, requires JSmooth)
ant doc - will create javadoc files
ant clean - remove all files created by the build script

4) For more information
=======================
Check out the doc folder. Don't forget to also run "ant doc" for creating the javadoc.
