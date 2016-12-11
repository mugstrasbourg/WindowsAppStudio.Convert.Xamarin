# WindowsAppStudio.Convert.Xamarin
Convert Windows App Studio app to Xamarin app (Android, iOS, UWP)

*Important*
This is the first version: the base works (the Xamarin projects are generated), but some features aren't yet supported.

Notes:
- I used the [Acquaint sample of Xamarin](https://github.com/xamarinhq/app-acquaint) as a basis of the Xamarin app.
- the generated Xamarin projects are using the Nuget package of the Github project [mugstrasbourg/waslibs.Xamarin](https://github.com/mugstrasbourg/waslibs.Xamarin).

The Nuget package is here:
```
https://www.nuget.org/packages/WindowsAppStudio.Convert.Xamarin
```
# What is it?
It's a convert tool to transform a [Windows App Studio](http://appstudio.windows.com) app into a Xamarin app.

# How to use it?
- create a [Windows App Studio](http://appstudio.windows.com) app
- download the code source (and unzip it)
- launch the .sln file (with Visual Studio on Windows)
- add the nuget "WindowsAppStudio.Convert.Xamarin" (available on nuget.org).
The Xamarin files are now created!
- go to the .Xamarin folder and launch the .Xamarin.sln file 
- restore Nuget packages
- unload the "...(portable)" project
- reload the "...(portable)" project
- build the solution

