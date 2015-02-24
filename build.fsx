#r @"packages/FAKE.3.17.6/tools/FakeLib.dll"
#load "build-helpers.fsx"
open Fake
open System
open System.IO
open System.Linq
open BuildHelpers
open Fake.XamarinHelper

Target "build-all" (fun () ->
    RestorePackages "SalesAppCrm.sln"

    MSBuild "SalesAppCrm.Core/bin/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "Any CPU") ] [ "SalesAppCrm.Core/SalesAppCrm.Core.csproj" ] |> ignore
    MSBuild "SalesAppCrm.Core.Tests/bin/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "Any CPU") ] [ "SalesAppCrm.Core.Tests/SalesAppCrm.Core.Tests.csproj" ] |> ignore
    MSBuild "SalesAppCrm.iOS/bin/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "iPhoneSimulator") ] [ "SalesAppCrm.iOS/SalesAppCrm.iOS.csproj" ] |> ignore
)

Target "run-tests" (fun () -> 
    RunNUnitTests "SalesAppCrm.Core.Tests/bin/Debug/SalesAppCrm.Core.Tests.dll" "SalesAppCrm.Core.Tests/bin/Debug/testresults.xml"
)

Target "ios-build" (fun () ->
    RestorePackages "SalesAppCrm.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "SalesAppCrm.iOS.csproj"
            Configuration = "Debug|iPhoneSimulator"
            Target = "Build"
        })
)

Target "ios-adhoc" (fun () ->
    RestorePackages "SalesAppCrm.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "SalesAppCrm.iOS.csproj"
            Configuration = "Ad-Hoc|iPhone"
            Target = "Build"
        })

    let appPath = Directory.EnumerateFiles(Path.Combine("src", "SalesAppCrm.iOS", "bin", "iPhone", "Ad-Hoc"), "*.ipa").First()

    TeamCityHelper.PublishArtifact appPath
)

Target "ios-appstore" (fun () ->
    RestorePackages "SalesAppCrm.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "SalesAppCrm.iOS.csproj"
            Configuration = "AppStore|iPhone"
            Target = "Build"
        })

    let outputFolder = Path.Combine("src", "SalesAppCrm.iOS", "bin", "iPhone", "AppStore")
    let appPath = Directory.EnumerateDirectories(outputFolder, "*.app").First()
    let zipFilePath = Path.Combine(outputFolder, "SalesAppCrm.iOS.zip")
    let zipArgs = String.Format("-r -y '{0}' '{1}'", zipFilePath, appPath)

    Exec "zip" zipArgs

    TeamCityHelper.PublishArtifact zipFilePath
)

Target "ios-uitests" (fun () ->
    let appPath = Directory.EnumerateDirectories(Path.Combine("src", "SalesAppCrm.iOS", "bin", "iPhoneSimulator", "Debug"), "*.app").First()

    RunUITests appPath
)

Target "ios-testcloud" (fun () ->
    RestorePackages "SalesAppCrm.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "SalesAppCrm.iOS.csproj"
            Configuration = "Debug|iPhone"
            Target = "Build"
        })

    let appPath = Directory.EnumerateFiles(Path.Combine("src", "SalesAppCrm.iOS", "bin", "iPhone", "Debug"), "*.ipa").First()

    getBuildParam "devices" |> RunTestCloudTests appPath
)

Target "android-build" (fun () ->
    RestorePackages "SalesAppCrm.sln"

    MSBuild "SalesAppCrm.Droid/bin/Release" "Build" [ ("Configuration", "Release") ] [ "SalesAppCrm.Droid.csproj" ] |> ignore
)

Target "android-package" (fun () ->
    AndroidPackage (fun defaults ->
        {defaults with
            ProjectPath = "SalesAppCrm.Droid/SalesAppCrm.Droid.csproj"
            Configuration = "Release"
            OutputPath = "SalesAppCrm.Droid/bin/Release"
        }) 
    |> AndroidSignAndAlign (fun defaults ->
        {defaults with
            KeystorePath = "generic.keystore"
            KeystorePassword = "tipcalc" // TODO: don't store this in the build script for a real app!
            KeystoreAlias = "tipcalc"
        })
    |> fun file -> TeamCityHelper.PublishArtifact file.FullName
)

Target "android-uitests" (fun () ->
    AndroidPackage (fun defaults ->
        {defaults with
            ProjectPath = "SalesAppCrm.Droid/SalesAppCrm.Droid.csproj"
            Configuration = "Release"
            OutputPath = "SalesAppCrm.Droid/bin/Release"
        }) |> ignore

    let appPath = Directory.EnumerateFiles(Path.Combine("src", "SalesAppCrm.Droid", "bin", "Release"), "*.apk", SearchOption.AllDirectories).First()

    RunUITests appPath
)

Target "android-testcloud" (fun () ->
    AndroidPackage (fun defaults ->
        {defaults with
            ProjectPath = "SalesAppCrm.Droid/SalesAppCrm.Droid.csproj"
            Configuration = "Release"
            OutputPath = "SalesAppCrm.Droid/bin/Release"
        }) |> ignore

    let appPath = Directory.EnumerateFiles(Path.Combine("src", "SalesAppCrm.Droid", "bin", "Release"), "*.apk", SearchOption.AllDirectories).First()

    getBuildParam "devices" |> RunTestCloudTests appPath
)

"build-all"
  ==> "run-tests"

"ios-build"
  ==> "ios-uitests"

"android-build"
  ==> "android-uitests"

"android-build"
  ==> "android-testcloud"

"android-build"
  ==> "android-package"

RunTarget() 