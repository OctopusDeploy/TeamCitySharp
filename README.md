# TeamCitySharp

* .NET Library to access TeamCity via their REST API.

Current Stable Version:
[![NuGet version (Octopus.TeamCitySharp)](https://img.shields.io/nuget/v/Octopus.TeamCitySharp.svg?style=flat-square)](https://www.nuget.org/packages/Octopus.TeamCitySharp/)

Latest Version:
[![NuGet version (Octopus.TeamCitySharp)](https://img.shields.io/nuget/vpre/Octopus.TeamCitySharp.svg?style=flat-square)](https://www.nuget.org/packages/Octopus.TeamCitySharp/)

For more information on TeamCity visit:
http://www.jetbrains.com/teamcity

## License

MIT

This is a forked version of [mavezeau/TeamCitySharp](https://github.com/mavezeau/TeamCitySharp) (released under MIT), which was forked from [stack72/TeamCitySharp](https://github.com/stack72/TeamCitySharp) (released under MIT).

## Installation

```powershell
install-package Octopus.TeamCitySharp
```

## Build Monitor

* There is a sample build monitor built with TeamCitySharp. It can be found at [TeamCityMonitor](https://github.com/stack72/TeamCityMonitor)

## Sample Usage

To get a list of projects

    var client = new TeamCityClient("localhost:81");
    client.Connect("admin", "qwerty");
    var projects = client.Projects.All();

To get a list of running builds

    var client = new TeamCityClient("localhost:81");
    client.Connect("admin", "qwerty");
    var builds = client.Builds.ByBuildLocator(BuildLocator.RunningBuilds());

## Connecting to a server

To connect as an authenticated user:

    var client = new TeamCityClient("localhost:81");
    // To use a https server
    // var client = new TeamCityClient("localhost", true);
    client.Connect("username", "password");

To connect as a Guest:

    var client = new TeamCityClient("localhost:81");
    client.ConnectAsGuest();

To connect with a access token (Since: 2019.1):

    // see https://www.jetbrains.com/help/teamcity/2019.2/authentication-modules.html#AuthenticationModules-tokenBasedAuth
    var client = new TeamCityClient("localhost:81");
    client.ConnectWithAccessToken("Token");

To use a previous rest api version:

    var client = new TeamCityClient("localhost:81");
    client.Connect("admin", "qwerty");
    client.UseVersion("7.0"); // 6.0, 7.0, 8.1, 9.0, 9.1, 10.0, 2017.1, 2017.2, 2018.1, latest

Use fields specializations: Extract complex objects for specified Fields

    // For each builds get only the Id, Number, Status and StartDate
    var buildField = BuildField.WithFields(id: true,number:true, status: true, startDate: true);
    var buildsFields = BuildsField.WithFields( buildField: buildField);
    var currentListBuild = client.Builds.GetFields(buildsFields.ToString()).ByBuildConfigId(currentProjectId);

Use fields specializations: Extract statistics from a build with one query

    // For a build get Statistics, id and the build number
    var propertyField = PropertyField.WithFields(name: true, value: true);
    var statisticsField = StatisticsField.WithFields(propertyField: propertyField,href:true, count:true);
    var buildField = BuildField.WithFields(id: true,number:true, statistics: statisticsField);
    var tempBuild = m_client.Builds.LastBuildByBuildConfigId(tempBuildConfig.Id);

## API Interaction Groups

There are many tasks that the TeamCity API can do for us. TeamCitySharp groups these tasks into specialist areas

* Builds
* BuildConfigs
* BuildInvestigations
* BuildQueue
* Projects
* ServerInformation
* Users
* Agents
* VcsRoots
* Changes
* Triggered
* LastChange
* BuildArtifacts
* Statistics

Each area has its own list of methods available


## Testing

This project uses [WireMock](https://github.com/WireMock-Net/WireMock.Net) to mock the TeamCity server during 
Integration Tests, using mapping files containing the request and response. This allows us to test without needing a 
real TeamCity server, avoiding a bunch of test flakiness and unplanned toil.

The downside of this is that if JetBrains changes the response, we won't find out about it until it's reported in production.
Given the low expectation of change for this code, and that JetBrains is pretty good at backwards compat, this is
an acceptable risk.

If we were to look at changing it so that we can automatically detect regressions, we'd need to setup terraform / 
infra-as-code to find out, which brings us back to the pain/maintenance overhead of using "real services" in tests.

# FAQ

Q: Would you recommend using the record/replay approach for other projects?
A: For other greenfield projects, I'd recommend the fluent approach. 

Q: Should I write new tests using the record/replay approach?
For this project, I'd say consistency is better, and we should use mapping files.

Q: How do I record/replay for a changed response?
A: If the response has changed, you can record the updated response by uncommenting the `//EnableProxyAndRecord()` 
method in the `Configuration` class. You'll also need to ensure you have a TeamCity setup in a way the test expects, 
with a bearer token in the appsettings.

Q: Why does record/replay keep overwriting files?
A: Given the hardcoded naming convention for the files, if the only thing that is different between requests is 
the body of the request, then it can overwrite the file. Assuming the (http) request is different, you can copy the 
overwritten file to a new one, and revert the changes to the original.  

Q: Why is replay picking up the wrong mapping file?
A: WireMock selects the mapping based on the number of matchers that are satisfied, and ranks them by percentage.
If you have a mapping file that is too generic, it may be selected over the one you want. (ie, if you have 3 matches in 
one file ,and 4 matches in another, it doesn't appear to be deterministic.)
Another option is to use [scenarios](https://github.com/WireMock-Net/WireMock.Net/wiki/Scenarios-and-States) to help it 
figure out what state the server is in.

Q: Why is there a bearer token in the mapping files?
A: Some test validate "does it work with admin user", and "does it throw an exception with an invalid user". 
Unfortunately, the only difference in these requests is the bearer token, so we need to have it in the mapping file.
Note that the bearer token is for a local TeamCity instance, and has been revoked.

