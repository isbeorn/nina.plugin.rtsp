using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("RTSP Client")]
[assembly: AssemblyDescription("A plugin that adds a new dock window to the imaging tab to show RTSP video streams")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Stefan Berg")]
[assembly: AssemblyProduct("NINA.Plugins")]
[assembly: AssemblyCopyright("Copyright ©  2022")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("b1b32467-2343-4d57-bcd9-a45e2773fa98")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("2.0.0.1")]
[assembly: AssemblyFileVersion("2.0.0.1")]

//The minimum Version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.1031")]

//Your plugin homepage - omit if not applicaple
[assembly: AssemblyMetadata("Homepage", "https://www.patreon.com/stefanberg/")]
//The license your plugin code is using
[assembly: AssemblyMetadata("License", "MPL-2.0")]
//The url to the license
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
//The repository where your pluggin is hosted
[assembly: AssemblyMetadata("Repository", "https://bitbucket.org/Isbeorn/nina.plugin.rtsp/src/master/")]

[assembly: AssemblyMetadata("ChangelogURL", "https://bitbucket.org/Isbeorn/nina.plugin.rtsp/src/master/NINA.Plugin.RTSP/Changelog.md")]

//Common tags that quickly describe your plugin
[assembly: AssemblyMetadata("Tags", "RTSP")]

//The featured logo that will be displayed in the plugin list next to the name
[assembly: AssemblyMetadata("FeaturedImageURL", "")]
//An example screenshot of your plugin in action
[assembly: AssemblyMetadata("ScreenshotURL", "")]
//An additional example screenshot of your plugin in action
[assembly: AssemblyMetadata("AltScreenshotURL", "")]
[assembly: AssemblyMetadata("LongDescription", @"
With this plugin you can show RTSP video streams using compatible video devices inside N.I.N.A.  
Head to the imaging tab, open the new RTSP Player panel that will be available and provide the details like username password and the url to the stream.  
If your camera does not require authentication, just leave the username and password fields blank.
  
The plugin is made possible by utilizing the [FFMPEG](https://www.ffmpeg.org/) solution to stream audio and video.  
Furthermore the video player integration is done using the great [FFMediaElement](https://github.com/unosquare/ffmediaelement) project.  
")]
