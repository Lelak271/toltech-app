using System.Windows;
using System.Windows.Markup;

[assembly:ThemeInfo(
    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
                                                //(used if a resource is not found in the page,
                                                // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
                                                //(used if a resource is not found in the page,
                                                // app, or any theme specific resource dictionaries)
)]


//[assembly: XmlnsPrefix(
//    "http://schemas.toltech/ui",
//    "ui"
//)]

//[assembly: XmlnsDefinition(
//    "http://schemas.toltech/ui",
//    "Toltech.App.Resources.Lang"
//)]

//[assembly: XmlnsDefinition(
//    "http://schemas.toltech/ui",
//    "Toltech.App.Converters"
//)]

//[assembly: XmlnsDefinition(
//    "http://schemas.toltech/ui",
//    "Toltech.App.FrontEnd.Controls"
//)]

[assembly: XmlnsDefinition(
    "http://schemas.toltech/ui",
    "Toltech.App.FrontEnd.Behaviors"
)]
