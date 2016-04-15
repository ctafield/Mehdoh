using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if MEHDOH_PRO
[assembly: AssemblyTitle("Mehdoh Unity")]
#elif MEHDOH_FREE
[assembly: AssemblyTitle("Little Mehdoh")]
#else
[assembly: AssemblyTitle("Mehdoh")]
#endif

[assembly: AssemblyDescription("Social network client for Windows Phone 8")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("My Own Limited")]

#if MEHDOH_PRO
[assembly: AssemblyProduct("Mehdoh Unity")]
#elif MEHDOH_FREE
[assembly: AssemblyProduct("Little Mehdoh")]
#else
[assembly: AssemblyProduct("Mehdoh")]
#endif

[assembly: AssemblyCopyright("Copyright © My Own Limited 2013")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("2eb78467-eda8-4d2a-9037-30cde8eade0a")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
// WP8 version information
[assembly: AssemblyVersion("9.37.2611.8")]
[assembly: AssemblyFileVersion("9.37.2611.8")]
[assembly: NeutralResourcesLanguageAttribute("en-GB")]
