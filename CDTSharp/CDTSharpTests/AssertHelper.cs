using CDTSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharpTests
{
    using static CDT;
    public static class AssertHelper
    {
        public static void Equal(CDTTriangle expected, List<CDTTriangle> tris, int index, string? message = null)
        {
            for (int i = 0; i < 3; i++)
            {
                var actual = tris[index];
                Assert.True(
                     expected.indices[i] == actual.indices[i],
                     $"Triangle[{index}].indices[{i}] not equal: expected {expected.indices[i]}, actual {actual.indices[i]}"
                 );

                Assert.True(
                    expected.adjacent[i] == actual.adjacent[i],
                    $"Triangle[{index}].adjacent[{i}] not equal: expected {expected.adjacent[i]}, actual {actual.adjacent[i]}"
                );
            }
        }

        public static void HasTwin(List<CDTTriangle> tris, int index)
        {
            CDTTriangle tri = tris[index];
            bool found = false;

            for (int i = 0; i < 3; i++)
            {
                int a = tri.indices[i];
                int b = tri.indices[(i + 1) % 3];

                for (int j = 0; j < tris.Count; j++)
                {
                    if (index == j) continue;

                    int oppEdge = tris[j].IndexOf(b, a); // reversed edge
                    if (oppEdge != NO_INDEX)
                    {
                        found = true;

                        // Check that tri.adjacent[i] points to j
                        Assert.True(
                            tri.adjacent[i] == j,
                            $"Triangle[{index}].adjacent[{i}] should point to twin triangle {j} (shared edge {a}-{b})"
                        );

                        // Check that j's opposite edge points back to index
                        CDTTriangle twin = tris[j];
                        Assert.True(
                            twin.adjacent[oppEdge] == index,
                            $"Twin triangle[{j}].adjacent[{oppEdge}] should point back to triangle {index} (shared edge {b}-{a})"
                        );

                        return; // found and verified
                    }
                }
            }

            Assert.True(found, $"Triangle[{index}] has no twin for any of its edges");
        }
    }
}
