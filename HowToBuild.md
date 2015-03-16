How To Build Google Calendar Connectors

# How To Build Google Calendar Connectors #
## Building Google Calendar Connector Web / Sync Service ##

### Prerequisites ###
  * Visual Studio 2005 SP1 (or later)
  * Install NUnit for running unit tests
  * Install Microsoft .NET Runtime 2.x

### Build Steps ###
The Google Web Service and Sync Service are built using the _GoogleCalendarConnectors.sln_ Microsoft Visual Studio solution file. The important projects in the solution are:
  * **GoogleCalendarConnectorSyncService.installer** - Builds the Sync Service Installer
  * **GoogleCalendarConnectorWebService.installer** - Builds the Web Service Installer
  * **unittests** - Builds the unit tests for all of the connectors. To run the unit tests, load the _/connectors/cs/src/VS2005/UnitTests/unittest.nunit_ config file into the NUnit GUI.

The documentation in _/connectors/cs/docs_ is built with Microsoft's Sandcastle help file builder, and the Sandcastle Help File Builder GUI.  The config file is connectors/cs/src/VS2005/MakeDocs.shfb_. (If you intend to build the docs, please see the notes at the bottom of this page.)_

## Building Google Calendar Connector Plugin ##

### Prerequisites ###
  * Install JDK 6 or higher - Check http://java.sun.com for more details.
  * Install ANT - You need a recent version of ANT installed (1.7.0 or higher). If you have ant pre-installed on your machine, you can enter "ant -version" to check the version number. See http://ant.apache.org/manual/install.html for more details on how to successfully setup ANT.
  * Download Dependencies - This application depends on a couple external libraries and applications. Please read the MISSING.txt in
    * lib
    * antlib and
    * testlib
> and place the required files in those directories. Note that the files in antlib are optional, but you will have to delete the "JSmooth"-section from
> the build.xml if you choose not to use them.

### Build Steps ###
The following ant commands should be useful:

  * ant compile (same as just running ant) - will compile the sources and place a jar file in the dist subfolder
  * ant test - will compile and execute the unit tests
  * ant dist - will build the windows executables and package them with the install/uninstall batchfiles and a sample config.txt (this OPTIONAL step will even work in a Linux build system, requires JSmooth)
  * ant doc - will create javadoc files
  * ant clean - remove all files created by the build script

For more information, check out the doc folder. Don't forget to also run "ant doc" for creating the javadoc.

## Submitting changes back to the project ##

If you've made improvements or fixed bugs in the connectors we'd appreciate your submission - just send a patch with the changes to one of the project owners. If you have large improvements to make, it may be a good idea to announce it in the Discussion Group before you spend a lot of time on it to make sure it fits in with the overall project goals.

## Building the Documentation ##

If you use Sandcastle to build the documentation, you may find that it gets confused by Subversion's .svn files and reports that the build has failed. (It may complain, for instance, that it .) One workaround is to rename _cs/docs_ to _cs/olddocs_. After the build completes, manually move all the generated files from _docs_ to _olddocs_, and then rename _olddocs_ back to _docs_. Finally, _svn add_ any new files and _svn delete_ any that no longer exist.