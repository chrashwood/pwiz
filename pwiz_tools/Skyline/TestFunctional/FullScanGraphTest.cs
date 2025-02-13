﻿/*
 * Original author: Don Marsh <donmarsh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2014 University of Washington - Seattle, WA
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.Chemistry;
using pwiz.MSGraph;
using pwiz.Skyline.Controls.Graphs;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util.Extensions;
using pwiz.SkylineTestUtil;
using ZedGraph;

namespace pwiz.SkylineTestFunctional
{
    [TestClass]
    public class FullScanGraphTest : AbstractFunctionalTestEx
    {
        [TestMethod]
        public void TestFullScanGraph()
        {
            Run(@"TestData\Results\BlibDriftTimeTest.zip");
        }

        protected override void DoTest()
        {
            Settings.Default.TransformTypeChromatogram = TransformChrom.interpolated.ToString();
            OpenDocument("BlibDriftTimeTest.sky");
            ImportResults("ID12692_01_UCA168_3727_040714" + ExtensionTestContext.ExtMz5);
            FindNode("453");
            WaitForGraphs();

            CloseSpectrumGraph();

            // Check ion mobility details display
            var expectedIonMobility =
                IonMobilityFilter.GetIonMobilityFilter(3.48, eIonMobilityUnits.drift_time_msec, 0, null);
            for (var loop = 0; loop < 4; loop++)
            {
                bool wantCCS = loop < 2;
                bool wantIM = loop % 2 == 0;
                RunUI(() => SkylineWindow.ShowIonMobility = wantIM);
                RunUI(() => SkylineWindow.ShowCollisionCrossSection = wantCCS);
                WaitForGraphs();
                var graphChrom = SkylineWindow.GetGraphChrom("ID12692_01_UCA168_3727_040714");
                var pane = graphChrom.GraphItem as MSGraphPane;
                var annotation = pane?.GetAnnotationLabelStrings().First() ?? string.Empty;
                AssertEx.AreEqual(wantIM, annotation.Contains(ChromGraphItem.FormatIonMobilityValue(expectedIonMobility)),
                    "did not find expected IMS information display");
                AssertEx.AreEqual(wantCCS, annotation.Contains(ChromGraphItem.FormatCollisionCrossSectionValue(expectedIonMobility)),
                    " did not find expected CCS information display");
            }

            // Simulate click on a peak in GraphChromatogram form.
            ClickChromatogram(32.95, 134.6);
            TestScale(452, 456, 0, 250);
            ClickChromatogram(33.23, 27.9);
            TestScale(452, 456, 0, 400);
            WaitForOpenForm<GraphFullScan>();   // For localization testing

            // Check arrow navigation.
            ClickForward(33.25, 0);
            TestScale(452, 456, 0, 200);
            ClickBackward(33.23, 27.9);
            TestScale(452, 456, 0, 400);

            // Check scan type selection.
            SetScanType(ChromSource.fragment, 33.24, 27.9);
            TestScale(453, 457, 0, 80);
            SetScanType(ChromSource.ms1, 33.23, 27.9);
            TestScale(452, 456, 0, 400);

            // Check filtered spectrum.
            SetFilter(true);
            TestScale(452, 456, 0, 40);
            SetFilter(false);
            TestScale(452, 456, 0, 400);

            // Check zoomed spectrum.
            SetZoom(false);
            TestScale(0, 2000, 0, 5100);
            SetZoom(true);
            TestScale(452, 456, 0, 400);

            // Check zoomed heatmap.
            SetSpectrum(false);
            TestScale(452, 456, 2.61, 4.34);
            WaitForOpenForm<GraphFullScan>();   // For localization testing

            // Check filtered heatmap.
            SetFilter(true);
            TestScale(452, 456, 3.2, 3.8);
            SetZoom(false);
            TestScale(0, 2000, 3.2, 3.8);
            SetFilter(false);
            TestScale(0, 2000, 0, 15);
            SetZoom(true);
            TestScale(452, 456, 2.61, 4.34);

            // Check click on ion label.
            SetSpectrum(true);
            SetZoom(false);
            SetScanType(ChromSource.fragment, 33.23, 27.9);
            ClickFullScan(517, 1000);
            TestScale(516, 520, 0, 80);

            //Check the rank and annotate functionality if we run in 
            //onscreen mode
            SetShowAnnotations(true);
            if (!Skyline.Program.SkylineOffscreen)
            {
                var ions1 = new []
                {
                    "y4" + TextUtil.SEPARATOR_SPACE + string.Format(@"({0})",string.Format(Resources.AbstractSpectrumGraphItem_GetLabel_rank__0__, 9))
                };
                var ions2 = new[]
                {
                    "y1" + TextUtil.SEPARATOR_SPACE + string.Format(@"({0})",string.Format(Resources.AbstractSpectrumGraphItem_GetLabel_rank__0__, 1)),
                    "y1++" + TextUtil.SEPARATOR_SPACE + string.Format(@"({0})",string.Format(Resources.AbstractSpectrumGraphItem_GetLabel_rank__0__, 2)),
                    "y5++" + TextUtil.SEPARATOR_SPACE + string.Format(@"({0})",string.Format(Resources.AbstractSpectrumGraphItem_GetLabel_rank__0__, 3)),
                    "y7" + TextUtil.SEPARATOR_SPACE + string.Format(@"({0})",string.Format(Resources.AbstractSpectrumGraphItem_GetLabel_rank__0__, 6)),
                    "y11" + TextUtil.SEPARATOR_SPACE + string.Format(@"({0})",string.Format(Resources.AbstractSpectrumGraphItem_GetLabel_rank__0__, 21))
                };

                TestAnnotations(ions1);
                SetZoom(false);
                TestAnnotations(ions2);
                RunUI(() => SkylineWindow.GraphFullScan.SetMzRange(500, 700));
                ClickFullScan(618, 120);
                TestScale(617, 621, 0, 60);
                SetShowAnnotations(false);
                TestAnnotations(new[] {"y4"});
            }
            SetShowAnnotations(false);

            // Check split graph
            ShowSplitChromatogramGraph(true);
            ClickChromatogram(33.11, 15.055, PaneKey.PRODUCTS);
            TestScale(529, 533, 0, 50);
            ClickChromatogram(33.06, 68.8, PaneKey.PRECURSORS);
            TestScale(452, 456, 0, 300);
        }

        private static void ClickFullScan(double x, double y)
        {
            RunUI(() => SkylineWindow.GraphFullScan.TestMouseClick(x, y));
        }

        private static void TestScale(double xMin, double xMax, double yMin, double yMax)
        {
            RunUI(() =>
            {
                double xAxisMin = SkylineWindow.GraphFullScan.XAxisMin;
                double xAxisMax = SkylineWindow.GraphFullScan.XAxisMax;
                double yAxisMin = SkylineWindow.GraphFullScan.YAxisMin;
                double yAxisMax = SkylineWindow.GraphFullScan.YAxisMax;

                Assert.IsTrue(xMin - xAxisMin >= 0 &&
                              xMin - xAxisMin < (xMax - xMin)/4,
                              "Expected x minimum {0}, got {1}", xMin, xAxisMin);
                Assert.IsTrue(xAxisMax - xMax >= 0 &&
                              xAxisMax - xMax < (xMax - xMin)/4,
                              "Expected x maximum {0}, got {1}", xMax, xAxisMax);
                Assert.IsTrue(yMin - yAxisMin >= 0 &&
                              yMin - yAxisMin < (yMax - yMin)/4,
                              "Expected y minimum {0}, got {1}", yMin, yAxisMin);
                Assert.IsTrue(yAxisMax - yMax >= 0 &&
                              yAxisMax - yMax < (yMax - yMin)/4,
                              "Expected y maximum {0}, got {1}", yMax, yAxisMax);
            });
        }

        private static void ClickForward(double x, double y)
        {
            RunUI(() => SkylineWindow.GraphFullScan.ChangeScan(1));
            CheckFullScanSelection(x, y);
        }

        private static void ClickBackward(double x, double y)
        {
            RunUI(() => SkylineWindow.GraphFullScan.ChangeScan(-1));
            CheckFullScanSelection(x, y);
        }

        private static void SetScanType(ChromSource source, double x, double y)
        {
            RunUI(() => SkylineWindow.GraphFullScan.SelectScanType(source));
            CheckFullScanSelection(x, y);
        }

        private static void SetShowAnnotations(bool isChecked)
        {
            RunUI(() => SkylineWindow.GraphFullScan.SetShowAnnotations(isChecked));
        }

        private static void TestAnnotations(string[] annotationText)
        {
            var graphLabels = SkylineWindow.GraphFullScan.ZedGraphControl.GraphPane.GraphObjList.OfType<TextObj>()
                .Select(label => label.Text).ToHashSet();
            Assert.IsTrue(annotationText.All(txt => graphLabels.Contains(txt)));
        }
        private static void SetFilter(bool isChecked)
        {
            RunUI(() => SkylineWindow.GraphFullScan.SetFilter(isChecked));
        }

        private static void SetZoom(bool isChecked)
        {
            RunUI(() => SkylineWindow.GraphFullScan.SetZoom(isChecked));
        }

        private static void SetSpectrum(bool isChecked)
        {
            RunUI(() => SkylineWindow.GraphFullScan.SetSpectrum(isChecked));
        }
    }
}
