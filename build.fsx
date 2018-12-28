#load "packages/Be.Vlaanderen.Basisregisters.Build.Pipeline/Content/build-generic.fsx"

open Fake
open ``Build-generic``

let dockerRepository = "example-registry"
let assemblyVersionNumber = (sprintf "2.0.0.%s")
let nugetVersionNumber = (sprintf "2.0.%s")

let build = buildSolution assemblyVersionNumber
let publish = publishSolution assemblyVersionNumber
let pack = packSolution nugetVersionNumber
let push = push dockerRepository
let containerize = containerize dockerRepository


Target "Clean" (fun _ ->
  CleanDir buildDir
)

// Solution -----------------------------------------------------------------------

Target "Build_Solution" (fun _ -> build "ExampleRegistry")

Target "Test_Solution" (fun _ ->
  [
    "test" @@ "ExampleRegistry.Tests"
  ] |> List.iter testWithDotNet)

Target "Publish_Solution" (fun _ -> publish "ExampleRegistry")

Target "Pack_Solution" (fun _ -> pack "ExampleRegistry")

Target "Containerize_Api" (fun _ -> containerize "ExampleRegistry.Api" "api")
Target "PushContainer_Api" (fun _ -> push "api")

// --------------------------------------------------------------------------------

Target "Build" DoNothing
Target "Test" DoNothing
Target "Publish" DoNothing
Target "Pack" DoNothing
Target "Containerize" DoNothing
Target "Push" DoNothing

// The buildserver passes in `BITBUCKET_BUILD_NUMBER` as an integer to version the results
// and `BUILD_DOCKER_REGISTRY` to point to a Docker registry to push the resulting Docker images.

// NpmInstall
// Run an `npm install` to setup Commitizen and Semantic Release.

// DotNetCli
// Checks if the requested .NET Core SDK and runtime version defined in global.json are available.
// We are pedantic about these being the exact versions to have identical builds everywhere.

// Clean
// Make sure we have a clean build directory to start with.

// Restore
// Restore dependencies for debian.8-x64 and win10-x64 using dotnet restore and Paket.

// Build
// Builds the solution in Release mode with the .NET Core SDK and runtime specified in global.json
// It builds it platform-neutral, debian.8-x64 and win10-x64 version.

// Test
// Runs `dotnet test` against the test projects.

// Publish
// Runs a `dotnet publish` for the debian.8-x64 and win10-x64 version as a self-contained application.
// It does this using the Release configuration.

// Pack
// Packs the solution using Paket in Release mode and places the result in the dist folder.
// This is usually used to build documentation NuGet packages.

// Containerize
// Executes a `docker build` to package the application as a docker image. It does not use a Docker cache.
// The result is tagged as latest and with the current version number.

// DockerLogin
// Executes `ci-docker-login.sh`, which does an aws ecr login to login to Amazon Elastic Container Registry.
// This uses the local aws settings, make sure they are working!

// Push
// Executes `docker push` to push the built images to the registry.

"NpmInstall"         ==> "Build"
"DotNetCli"          ==> "Build"
"Clean"              ==> "Build"
"Restore"            ==> "Build"
"Build_Solution"     ==> "Build"

"Build"              ==> "Test"
"Test_Solution"      ==> "Test"

"Test"               ==> "Publish"
"Publish_Solution"   ==> "Publish"

"Publish"            ==> "Pack"
"Pack_Solution"      ==> "Pack"

"Publish"            ==> "Containerize"
"Containerize_Api"   ==> "Containerize"
// Possibly add more projects to containerize here

"Containerize"       ==> "Push"
"DockerLogin"        ==> "Push"
"PushContainer_Api"  ==> "Push"
// Possibly add more projects to push here

// By default we build & test
RunTargetOrDefault "Test"