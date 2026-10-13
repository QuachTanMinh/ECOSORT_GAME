using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace EcoSortQuest.Tests
{
    public sealed class WasteCatalogTests
    {
        [Test] public void CatalogHasEighteenUniqueEducationalItems()
        {
            Assert.AreEqual(18,WasteCatalog.All.Count);
            Assert.AreEqual(18,WasteCatalog.All.Select(x=>x.Id).Distinct().Count());
            foreach(WasteType type in System.Enum.GetValues(typeof(WasteType)))
                Assert.AreEqual(6,WasteCatalog.All.Count(x=>x.Type==type));
            Assert.IsTrue(WasteCatalog.All.All(x=>!string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.Lesson)));
        }
        [Test] public void DeckKeepsRequestedCategoriesAndAvoidsEarlyRepeats()
        {
            var types=Enumerable.Repeat(WasteType.Paper,6).ToArray();
            var deck=WasteCatalog.Deck(types,3,123);
            Assert.AreEqual(6,deck.Select(x=>x.Id).Distinct().Count());
            Assert.IsTrue(deck.All(x=>x.Type==WasteType.Paper));
            Assert.AreEqual(deck.Select(x=>x.Id),WasteCatalog.Deck(types,3,123).Select(x=>x.Id));
        }
        [Test] public void ConveyorWrapPreservesTravelAcrossEitherEdge()
        {
            Assert.AreEqual(-4.15f,ConveyorMotion.Advance(4.25f,.3f),.001f);
            Assert.AreEqual(4.15f,ConveyorMotion.Advance(-4.25f,-.3f),.001f);
        }
        [Test] public void EveryModelHasUsablePickupColliderAndSelection()
        {
            var root=new GameObject("Catalog test"); var world=new WorldFactory(root.transform);
            try
            {
                foreach(var definition in WasteCatalog.All)
                {
                    var item=world.CatalogItem(definition,Vector3.zero);
                    Assert.AreEqual(definition.Type,item.type);
                    Assert.Greater(item.GetComponent<BoxCollider>().size.magnitude,.3f);
                    item.Select(true); Assert.IsTrue(item.selectionMarker.activeSelf);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                // EditMode cannot use delayed Destroy; runtime factory owns these materials.
                var field=typeof(WorldFactory).GetField("materials",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                foreach(var material in (System.Collections.Generic.List<Material>)field.GetValue(world)) Object.DestroyImmediate(material);
            }
        }
    }
}
