using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TeamCitySharp.DomainEntities;
using TeamCitySharp.Fields;
using TeamCitySharp.Locators;

namespace TeamCitySharp
{
    [TestFixture]
    public class Foo
    {
        [Test]
        public void Bar()
        {
            var client = new TeamCityClient("api.build.octopushq.com", true);
            client.ConnectWithAccessToken("eyJ0eXAiOiAiVENWMiJ9.YVdpNktzVWhzQzlUNXVsUGlxSjFVNGVaTmNn.MDkzMzQ3MjktOTExZS00OTI4LThmNzEtZTIwZGZmOTRjY2Vj");
            var buildConfigId = "OctopusDeploy_OctopusServer_TeamCorePlatform_ChainFull";

            var triggeredBuildField = BuildField.WithFields(id: true);
            var artifactsField = ArtifactsField.WithFields(href: true);
            var triggeredField = TriggeredField.WithFields(build: triggeredBuildField);
            var buildField = BuildField.WithFields(id: true, number:true, status: true, startDate: true, triggered: triggeredField, artifacts: artifactsField, buildTypeId: true, buildTypeInternalId: true );
            var buildsFields = BuildsField.WithFields(buildField: buildField);
            var currentBuildsList = client.Builds
                .GetFields(buildsFields.ToString())
                .ByBuildConfigId(buildConfigId)
                .Take(1)
                .Select(x => new { x.Id, x.Number, x.Status, RootBuildId = x.Triggered?.Build?.Id, x.Artifacts, x.BuildTypeId, x.BuildTypeInternalId });

            //https://build.octopushq.com/app/rest/testOccurrences?locator=build:(id:16261047),count:4999&fields=count,href,nexthref,ignored,passed,testOccurrence(id,name,status,duration,href,invocations)
            var tests = client.Tests
                .GetFields("count,href,nexthref,ignored,passed,testOccurrence(id,name,status,duration,href,))")
                .All(BuildLocator.WithId(16301742), count: 4999);

            
            //?fields=id,test(id,testOccurrences),invocations(count,href,status,testCounters,testOccurrence)
            
            var multipleInvocations = tests
                .SelectMany(testOccurrences => testOccurrences.TestOccurrence)
                .Where(testOccurrence => testOccurrence.Invocations != null)
                .Select(testOccurrence => new { TestOccurrence = testOccurrence, DistinctBuildCount = testOccurrence.Invocations.TestOccurrence.Select(GetBuildIdFromTestOccurrence).Distinct().Count(), InvocationCount = testOccurrence.Invocations.Count })
                //.Where(x => x.DistinctBuildCount > 1)
                .ToList();
            
            var dupes = multipleInvocations.Where(x => x.DistinctBuildCount != x.InvocationCount).ToList();
            var nonDupes = multipleInvocations.Where(x => x.DistinctBuildCount == x.InvocationCount).ToList();
            
            foreach (var r in multipleInvocations)
            {
                Console.WriteLine(r.TestOccurrence.Name);    
            }
            
        }

        private string GetBuildIdFromTestOccurrence(TestOccurrence testOccurrence)
        {
            var buildLocator = testOccurrence.Id.Split(',')[0];
            buildLocator = buildLocator.Replace("build:(", string.Empty);
            buildLocator = buildLocator.Replace(")", string.Empty);
            return buildLocator.Replace("id:", string.Empty);
        }
    }
}
