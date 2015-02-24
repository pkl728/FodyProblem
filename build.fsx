#r @"packages/FAKE.3.17.6/tools/FakeLib.dll"
#load "build-helpers.fsx"
open Fake
open System
open System.IO
open System.Linq
open BuildHelpers
open Fake.XamarinHelper

Target "build-all" (fun () ->
    RestorePackages "FodyProblem.sln"

    MSBuild "FodyProblem.Core/bin/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "Any CPU") ] [ "FodyProblem.Core/FodyProblem.Core.csproj" ] |> ignore
    MSBuild "FodyProblem.Core.Tests/bin/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "Any CPU") ] [ "FodyProblem.Core.Tests/FodyProblem.Core.Tests.csproj" ] |> ignore
    MSBuild "FodyProblem.iOS/bin/Debug" "Build" [ ("Configuration", "Debug"); ("Platform", "iPhoneSimulator") ] [ "FodyProblem.iOS/FodyProblem.iOS.csproj" ] |> ignore
)

Target "run-tests" (fun () -> 
    RunNUnitTests "FodyProblem.Core.Tests/bin/Debug/FodyProblem.Core.Tests.dll" "FodyProblem.Core.Tests/bin/Debug/testresults.xml"
)

Target "ios-build" (fun () ->
    RestorePackages "FodyProblem.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "FodyProblem.iOS.csproj"
            Configuration = "Debug|iPhoneSimulator"
            Target = "Build"
        })
)

Target "ios-adhoc" (fun () ->
    RestorePackages "FodyProblem.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "FodyProblem.iOS.csproj"
            Configuration = "Ad-Hoc|iPhone"
            Target = "Build"
        })

    let appPath = Directory.EnumerateFiles(Path.Combine("src", "FodyProblem.iOS", "bin", "iPhone", "Ad-Hoc"), "*.ipa").First()

    TeamCityHelper.PublishArtifact appPath
)

Target "ios-appstore" (fun () ->
    RestorePackages "FodyProblem.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "FodyProblem.iOS.csproj"
            Configuration = "AppStore|iPhone"
            Target = "Build"
        })

    let outputFolder = Path.Combine("src", "FodyProblem.iOS", "bin", "iPhone", "AppStore")
    let appPath = Directory.EnumerateDirectories(outputFolder, "*.app").First()
    let zipFilePath = Path.Combine(outputFolder, "FodyProblem.iOS.zip")
    let zipArgs = String.Format("-r -y '{0}' '{1}'", zipFilePath, appPath)

    Exec "zip" zipArgs

    TeamCityHelper.PublishArtifact zipFilePath
)

Target "ios-uitests" (fun () ->
    let appPath = Directory.EnumerateDirectories(Path.Combine("src", "FodyProblem.iOS", "bin", "iPhoneSimulator", "Debug"), "*.app").First()

    RunUITests appPath
)

Target "ios-testcloud" (fun () ->
    RestorePackages "FodyProblem.sln"

    iOSBuild (fun defaults ->
        {defaults with
            ProjectPath = "FodyProblem.iOS.csproj"
            Configuration = "Debug|iPhone"
            Target = "Build"
        })

    let appPath = Directory.EnumerateFiles(Path.Combine("src", "FodyProblem.iOS", "bin", "iPhone", "Debug"), "*.ipa").First()

    getBuildParam "devices" |> RunTestCloudTests appPath
)

Target "android-build" (fun () ->
    RestorePackages "FodyProblem.sln"

    MSBuild "FodyProblem.Droid/bin/Release" "Build" [ ("Configuration", "Release") ] [ "FodyProblem.Droid.csproj" ] |> ignore
)

Target "android-package" (fun () ->
    AndroidPackage (fun defaults ->
        {defaults with
            ProjectPath = "FodyProblem.Droid/FodyProblem.Droid.csproj"
            Configuration = "Release"
            OutputPath = "FodyProblem.Droid/bin/Release"
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
            ProjectPath = "FodyProblem.Droid/FodyProblem.Droid.csproj"
            Configuration = "Release"
            OutputPath = "FodyProblem.Droid/bin/Release"
        }) |> ignore

    let appPath = Directory.EnumerateFiles(Path.Combine("src", "FodyProblem.Droid", "bin", "Release"), "*.apk", SearchOption.AllDirectories).First()

    RunUITests appPath
)

Target "android-testcloud" (fun () ->
    AndroidPackage (fun defaults ->
        {defaults with
            ProjectPath = "FodyProblem.Droid/FodyProblem.Droid.csproj"
            Configuration = "Release"
            OutputPath = "FodyProblem.Droid/bin/Release"
        }) |> ignore

    let appPath = Directory.EnumerateFiles(Path.Combine("src", "FodyProblem.Droid", "bin", "Release"), "*.apk", SearchOption.AllDirectories).First()

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