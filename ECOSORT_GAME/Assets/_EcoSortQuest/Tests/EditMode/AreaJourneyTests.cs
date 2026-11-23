using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace EcoSortQuest.Tests
{
    public sealed class AreaJourneyTests
    {
        string player;
        [SetUp] public void Setup(){player="test-areas-"+Guid.NewGuid().ToString("N");}
        [TearDown] public void Cleanup(){PlayerPrefs.DeleteKey(AreaJourneyStore.Key(player));PlayerPrefs.Save();}
        [Test] public void EighteenAuthoredMissionsAreValidAndNewLocationsHaveThreeStepRoutes()
        {
            Assert.AreEqual(18,AreaMissions.All.Length);Assert.AreEqual(18,AreaMissions.All.Select(m=>m.Id).Distinct().Count());
            Assert.AreEqual(9,AreaMissions.MainJourney.Length);
            foreach(ExtraEnvironment environment in new[]{ExtraEnvironment.Hospital,ExtraEnvironment.Supermarket,ExtraEnvironment.Beach})Assert.AreEqual(3,AreaMissions.ForEnvironment(environment).Length);
            foreach(var m in AreaMissions.All)
            {
                Assert.That(m.Items.Length,Is.InRange(4,18));Assert.AreEqual(m.Items.Length,m.Items.Distinct().Count());
                Assert.IsTrue(m.Items.All(id=>WasteCatalog.All.Any(w=>w.Id==id && w.AppearsIn(m.Context))),m.Id);
                if(m.Separate) {Assert.Contains("plastic-cup",m.Items);Assert.Contains("paper-bag",m.Items);}
                if(m.Context==LearningContext.School){Assert.AreEqual(3,m.Options.Length);Assert.That(m.Answer,Is.InRange(0,2));Assert.IsNotEmpty(m.Explanation);}
            }
            Assert.IsTrue(AreaMissions.MainJourney.Where(m=>m.Context==LearningContext.Home).All(m=>m.Items.Length==4));
            Assert.IsTrue(AreaMissions.MainJourney.Where(m=>m.Context==LearningContext.Park).All(m=>m.Items.Length>=8));
        }
        [TestCase(4,3,4,0,0,0)] [TestCase(4,4,4,0,0,3)]
        [TestCase(4,4,3,0,0,2)] [TestCase(4,4,4,1,0,2)]
        [TestCase(4,4,4,0,1,2)] [TestCase(4,4,4,0,3,1)]
        [TestCase(12,12,11,0,0,3)] [TestCase(12,12,8,0,0,1)]
        [TestCase(4,4,5,0,0,0)] [TestCase(0,0,0,0,0,0)]
        public void StarsRewardCompletionAccuracyAndIndependentDecisions(int total,int cleaned,int correct,int mistakes,int hints,int stars)
        {Assert.AreEqual(stars,AreaJourneyStore.Stars(total,cleaned,correct,mistakes,hints));}
        [Test] public void CompletionUnlocksInOrderAndIsIdempotentAcrossReloadAndWorseReplay()
        {
            var initial=AreaJourneyStore.Load(player);Assert.IsTrue(AreaJourneyStore.Unlocked(initial,AreaMissions.All[0]));
            Assert.IsFalse(AreaJourneyStore.Unlocked(initial,AreaMissions.All[1]));
            Assert.IsNull(AreaJourneyStore.Complete(player,AreaMissions.All[1],"skip",4,4,0,0,Array.Empty<string>()));
            Assert.IsNull(AreaJourneyStore.Complete(player,AreaMissions.All[0],"partial",3,3,0,0,Array.Empty<string>()));
            foreach(var mission in AreaMissions.MainJourney)
            {
                int n=mission.Items.Length;
                var record=AreaJourneyStore.Complete(player,mission,"session"+mission.Id,n,n,0,0,new[]{"Plastic","Metal","Paper","Organic"});
                Assert.AreEqual(3,record.bestStars);
                record=AreaJourneyStore.Complete(player,mission,"session"+mission.Id,n,n,0,0,Array.Empty<string>());Assert.AreEqual(1,record.completions);
                Assert.IsTrue(AreaJourneyStore.Unlocked(AreaJourneyStore.Load(player),mission));
            }
            var data=AreaJourneyStore.Load(player);Assert.AreEqual(27,data.missions.Sum(m=>m.bestStars));
            Assert.Contains("Nhận biết bốn vật liệu",data.badges);Assert.Contains("Người giữ góc xanh",data.badges);
            var worse=AreaJourneyStore.Complete(player,AreaMissions.All[0],"worse",4,1,2,4,Array.Empty<string>());
            Assert.AreEqual(3,worse.bestStars);Assert.AreEqual(2,worse.completions);Assert.AreEqual(3,AreaJourneyStore.Decorations(AreaJourneyStore.Load(player),LearningContext.Home));
        }
        [Test] public void EachNewLocationUnlocksOnlyItsOwnThreeMissionRoute()
        {
            foreach(var environment in new[]{ExtraEnvironment.Hospital,ExtraEnvironment.Supermarket,ExtraEnvironment.Beach})
            {
                var route=AreaMissions.ForEnvironment(environment);var data=AreaJourneyStore.Load(player);
                Assert.IsTrue(AreaJourneyStore.Unlocked(data,route[0]));Assert.IsFalse(AreaJourneyStore.Unlocked(data,route[1]));
                int n=route[0].Items.Length;
                Assert.IsNotNull(AreaJourneyStore.Complete(player,route[0],environment+"-first",n,n,0,0,Array.Empty<string>()));
                data=AreaJourneyStore.Load(player);Assert.IsTrue(AreaJourneyStore.Unlocked(data,route[1]));Assert.IsFalse(AreaJourneyStore.Unlocked(data,route[2]));
            }
        }
        [Test] public void SupermarketMissionLayoutsHaveDistinctSafeSpawnPoints()
        {
            var route=AreaMissions.ForEnvironment(ExtraEnvironment.Supermarket);
            Assert.AreEqual("01 • Lối đi mua sắm",route[0].Title);
            Assert.AreEqual("02 • Quầy thanh toán gọn",route[1].Title);
            Assert.AreEqual("03 • Siêu thị xanh",route[2].Title);
            for(int level=0;level<route.Length;level++)
            {
                var layout=ExtraEnvironmentInfo.Layout(ExtraEnvironment.Supermarket,level);
                Assert.AreEqual("extra-Supermarket-"+level,layout.Id);
                Assert.GreaterOrEqual(layout.Items.Length,route[level].Items.Length);
                Assert.AreEqual(layout.Items.Length,layout.Items.Select(p=>new Vector2(p.x,p.z)).Distinct().Count());
                Assert.IsTrue(layout.Items.All(p=>p.x < -4.5f || p.x > 4.5f || p.z < 4f || p.z > 7f));
            }
        }
        [Test] public void SupermarketScenesBuildWithClearSpawnPointsAndStageSpecificProps()
        {
            var previousAmbientMode=RenderSettings.ambientMode;var previousAmbientLight=RenderSettings.ambientLight;
            var expectedProps=new[]{
                new[]{"Grocery aisle shelf","Aisle number board","Shopping cart basket","Aisle direction arrow"},
                new[]{"Checkout counter","Checkout conveyor","Candy rack","Checkout number sign"},
                new[]{"Fresh produce display","Refrigerated display case","Pallet base","Hand pallet truck handle"}
            };
            try
            {
                for(int level=0;level<expectedProps.Length;level++)
                {
                    var mission=AreaMissions.ForEnvironment(ExtraEnvironment.Supermarket)[level];
                    var root=new GameObject("Supermarket scene test "+level);
                    var world=new WorldFactory(root.transform);
                    try
                    {
                        var layout=world.BuildExtraEnvironment(ExtraEnvironment.Supermarket,level);
                        Physics.SyncTransforms();
                        var names=root.GetComponentsInChildren<Transform>().Select(t=>t.name).ToArray();
                        foreach(string prop in expectedProps[level])Assert.Contains(prop,names);
                        Assert.AreEqual(mission.Zone,root.GetComponentsInChildren<TextMesh>().First(t=>t.name=="Expanded room title").text);
                        foreach(var point in layout.Items)
                        {
                            var blockers=Physics.OverlapCapsule(point+Vector3.up*.4f,point+Vector3.up*1.5f,.34f)
                                .Where(c=>c.transform.IsChildOf(root.transform) && c.bounds.max.y>.15f).ToArray();
                            Assert.IsEmpty(blockers,mission.Id+" spawn "+point+" intersects "+string.Join(",",blockers.Select(c=>c.name)));
                        }
                    }
                    finally
                    {
                        world.Dispose();UnityEngine.Object.DestroyImmediate(root);
                    }
                }
            }
            finally
            {
                RenderSettings.ambientMode=previousAmbientMode;RenderSettings.ambientLight=previousAmbientLight;
            }
        }
        [Test] public void AllNineExpandedScenesHaveMissionSignsAndUnblockedSpawnPoints()
        {
            var environments=new[]{ExtraEnvironment.Hospital,ExtraEnvironment.Supermarket,ExtraEnvironment.Beach};
            var expectedProps=new[]{
                new[]{
                    new[]{"Waiting room TV","Public information shelf","Waiting room plant"},
                    new[]{"Reception counter","Ticket machine body","Queue stanchion post"},
                    new[]{"Examination bed","Clinic privacy curtain","Medical trolley"}
                },
                new[]{
                    new[]{"Grocery aisle shelf","Aisle number board","Shopping cart basket","Aisle direction arrow"},
                    new[]{"Checkout counter","Checkout conveyor","Candy rack","Checkout number sign"},
                    new[]{"Fresh produce display","Refrigerated display case","Pallet base","Hand pallet truck handle"}
                },
                new[]{
                    new[]{"Beach umbrella canopy","Beach lounger seat","Sand castle base","Beach palm trunk"},
                    new[]{"Beach boardwalk deck","Beach cafe canopy","Cafe bicycle front wheel","Beach cafe sign"},
                    new[]{"Beach pier deck","Small beach boat hull","Beach fishing net frame","Lifeguard rescue marker"}
                }
            };
            var previousAmbientMode=RenderSettings.ambientMode;var previousAmbientLight=RenderSettings.ambientLight;
            try
            {
                for(int environmentIndex=0;environmentIndex<environments.Length;environmentIndex++)
                {
                    var environment=environments[environmentIndex];
                    var route=AreaMissions.ForEnvironment(environment);
                    var spawnSignatures=new string[route.Length];
                    for(int level=0;level<route.Length;level++)
                    {
                        var mission=route[level];
                        var root=new GameObject(environment+" scene test "+level);
                        var world=new WorldFactory(root.transform);
                        try
                        {
                            var layout=world.BuildExtraEnvironment(environment,level);
                            Physics.SyncTransforms();
                            Assert.AreEqual("extra-"+environment+"-"+level,layout.Id);
                            Assert.AreEqual(environment!=ExtraEnvironment.Beach,layout.Indoor);
                            Assert.GreaterOrEqual(layout.Items.Length,mission.Items.Length);
                            Assert.AreEqual(layout.Items.Length,layout.Items.Select(p=>new Vector2(p.x,p.z)).Distinct().Count());
                            string signature=string.Join(";",layout.Items.Select(p=>p.x.ToString("R")+","+p.z.ToString("R")).OrderBy(p=>p));
                            Assert.IsFalse(spawnSignatures.Take(level).Contains(signature),mission.Id+" reuses another level's spawn layout");
                            spawnSignatures[level]=signature;
                            Assert.IsTrue(layout.Items.All(p=>environment==ExtraEnvironment.Beach
                                ?p.x < -5.5f || p.x > 5.5f || p.z < 5f || p.z > 8f
                                :p.x < -4.5f || p.x > 4.5f || p.z < 4f || p.z > 7f),mission.Id+" has a spawn in the collection lane");

                            var transforms=root.GetComponentsInChildren<Transform>();
                            var names=transforms.Select(t=>t.name).ToArray();
                            foreach(string prop in expectedProps[environmentIndex][level])Assert.Contains(prop,names);
                            string titleName=environment==ExtraEnvironment.Beach?"Beach title":"Expanded room title";
                            string titleText=environment==ExtraEnvironment.Beach?mission.Title:mission.Zone;
                            Assert.AreEqual(titleText,root.GetComponentsInChildren<TextMesh>().First(t=>t.name==titleName).text);
                            if(environment==ExtraEnvironment.Beach)
                                foreach(var label in root.GetComponentsInChildren<TextMesh>())
                                {
                                    Assert.IsNotNull(label.transform.parent.GetComponent<Renderer>(),label.name+" needs a sign backing");
                                    Assert.Less(label.transform.localPosition.z,-label.transform.parent.localScale.z*.5f,label.name+" must sit in front of its board");
                                }
                            foreach(var point in layout.Items)
                            {
                                var blockers=Physics.OverlapCapsule(point+Vector3.up*.4f,point+Vector3.up*1.5f,.34f)
                                    .Where(c=>c.transform.IsChildOf(root.transform) && c.bounds.max.y>.15f).ToArray();
                                Assert.IsEmpty(blockers,mission.Id+" spawn "+point+" intersects "+string.Join(",",blockers.Select(c=>c.name)));
                            }
                        }
                        finally
                        {
                            world.Dispose();UnityEngine.Object.DestroyImmediate(root);
                        }
                    }
                }
            }
            finally
            {
                RenderSettings.ambientMode=previousAmbientMode;RenderSettings.ambientLight=previousAmbientLight;
            }
        }
        [Test] public void DiscoveryBadgesImprovementReviewAndCosmeticSettingsPersist()
        {
            AreaJourneyStore.Discover(player,"water-bottle");AreaJourneyStore.Discover(player,"water-bottle");AreaJourneyStore.Discover(player,"unknown");
            Assert.AreEqual(1,AreaJourneyStore.Load(player).discovered.Count);
            AreaJourneyStore.Complete(player,AreaMissions.All[0],"first",4,2,0,0,new[]{"Plastic"});
            AreaJourneyStore.Complete(player,AreaMissions.All[0],"improved",4,4,0,0,new[]{"Paper"});
            Assert.Contains("Tiến bộ từng ngày",AreaJourneyStore.Load(player).badges);
            AreaJourneyStore.RecordReview(player,new LearningSession{mode="Review",total=1,answers=new[]{new LearningAnswer{correct=false}}});
            Assert.IsFalse(AreaJourneyStore.Load(player).badges.Contains("Chăm chỉ ôn tập"));
            AreaJourneyStore.RecordReview(player,new LearningSession{mode="Review",total=1,answers=new[]{new LearningAnswer{correct=true}}});
            var data=AreaJourneyStore.Load(player);Assert.Contains("Chăm chỉ ôn tập",data.badges);
            data.plants=false;data.flowers=true;data.benches=false;AreaJourneyStore.Save(player,data);
            var loaded=AreaJourneyStore.Load(player);Assert.IsFalse(loaded.plants);Assert.IsTrue(loaded.flowers);Assert.IsFalse(loaded.benches);
        }
    }
}
