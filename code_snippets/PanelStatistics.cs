using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static scalagen2fssp_hifi_calibrator.MainForm;

namespace scalagen2fssp_hifi_calibrator
{
    public partial class MainForm : Form
    {
        private void button_Statistics_run_Click(object sender, EventArgs e)
        {
            SetConstants();

            // Step 1: Get Folder Paths
            string folderPathReal, folderPathSim;
            // Get the sensor variant (SMART or SLIM)
            
            List<string> realFileNames;
            GetFolderPaths(sensorVariant, out folderPathReal, out folderPathSim, out realFileNames);


            // Step 2: Initialize Default Parameters
            int matID = 29; // Default matID set
            double[] fixNs = new double[] { -1.0, 0.0, 0.0 }; // Default fixNs set 

            // Step 3: Read Snippets from REAL Folder
            SnippetFolderContents snippetFolderContents_Real = ReadSnippetsFolder(folderPathReal, matID, fixNs, true);

            // Step 4: Calculate the average surface normal vector based on the REAL snippet data
            fixNs = CalculateSurfaceNormal(snippetFolderContents_Real);

            // Step 5: Re-read REAL Folder Snippets with Updated fixNs
            snippetFolderContents_Real = ReadSnippetsFolder(folderPathReal, matID, fixNs, false);

            // Step 6: Initialize UI components such as charts and text boxes for displaying results
            Chart[] areaCharts, distanceCharts;
            TextBox[] detectionProbabilityTextBoxes_Real, detectionProbabilityTextBoxes_Sim;
            InitializeUIComponents(out areaCharts, out distanceCharts, out detectionProbabilityTextBoxes_Real, out detectionProbabilityTextBoxes_Sim);

            // Step 7: Process Snippets and Update Charts
            ProcessSnippets(snippetFolderContents_Real, fixNs, sensorVariant, areaCharts, distanceCharts, detectionProbabilityTextBoxes_Real, detectionProbabilityTextBoxes_Sim, realFileNames);

        }

        private void GetFolderPaths(int sensorVariant, out string folderPathReal, out string folderPathSim, out List<string> realFileNames)
        {
            string folderPath = textBox_Statistics_Folder1Path_SMART.Text;
            if (sensorVariant == 0) folderPath = textBox_Statistics_Folder1Path_SMART.Text;
            if (sensorVariant == 1) folderPath = textBox_Statistics_Folder1Path_SLIM.Text;
            if (sensorVariant == 2) folderPath = textBox_Statistics_Folder1Path_SATELLITE.Text;

            folderPathReal = folderPath + @"REAL\";
            folderPathSim = folderPath + @"SIM\";

            string[] realFilePaths = Directory.GetFiles(folderPathReal, "*.txt");
            realFileNames = realFilePaths.Select(path => Path.GetFileNameWithoutExtension(path)).ToList();

            // Read the .txt files from the folder --> To be updated
            // Temporary hardcoded path
            //folderPathReal = @"C:\Projects\Scala3_calibrator_DATA\StatisticsDataForComparison\txt";
        }

        private double[] CalculateSurfaceNormal(SnippetFolderContents snippetFolderContents_Real)
        {
            Vector averageVector = VectorForRealData(snippetFolderContents_Real);
            return new double[] { averageVector.X, averageVector.Y, averageVector.Z };
        }

        private void InitializeUIComponents(
            out Chart[] areaCharts,
            out Chart[] distanceCharts,
            out TextBox[] detectionProbabilityTextBoxes_Real,
            out TextBox[] detectionProbabilityTextBoxes_Sim)
        {
            areaCharts = new Chart[]
            {
                chart_Statistics_1_AREA,
                chart_Statistics_2_AREA,
                chart_Statistics_3_AREA
            };

            distanceCharts = new Chart[]
            {
                chart_Statistics_1_DIST,
                chart_Statistics_2_DIST,
                chart_Statistics_3_DIST
            };

            detectionProbabilityTextBoxes_Real = new TextBox[]
            {
                textBox_Statistics_1_DetectionProbability_Real,
                textBox_Statistics_2_DetectionProbability_Real,
                textBox_Statistics_3_DetectionProbability_Real
            };

            detectionProbabilityTextBoxes_Sim = new TextBox[]
            {
                textBox_Statistics_1_DetectionProbability_Sim,
                textBox_Statistics_2_DetectionProbability_Sim,
                textBox_Statistics_3_DetectionProbability_Sim
            };
        }

        private void ProcessSnippets(SnippetFolderContents snippetFolderContents_Real, double[] fixNs, int sensorVariant,
                                     Chart[] areaCharts, Chart[] distanceCharts,
                                     TextBox[] detectionProbabilityTextBoxes_Real, TextBox[] detectionProbabilityTextBoxes_Sim, List<string> realFileNames)
        {
            double fixedSlantRange = 0.0;
            bool fixSlantRange = false;

            Snippet_File accumulatedSnippet = new Snippet_File
            {
                Snippet_Point_List = new List<Snippet_Point>()
            };

            for (int fileIndex = 0; fileIndex < snippetFolderContents_Real.Snippet_File_List.Count && fileIndex < areaCharts.Length; fileIndex++)
            {
                Snippet_File snippet_file_Real = snippetFolderContents_Real.Snippet_File_List[fileIndex];

                Snippet_File snippet_file_Sim = new Snippet_File();
                snippet_file_Sim.Snippet_Point_List = new List<Snippet_Point>(); // Initialize the list to hold simulated accumulated points when multiplied by 30.

                //int minHorPointToCalculate = (sensorVariant == 0) ? (NUM_SLOTS_0 - 1) : (NUM_SLOTS_1 - 1); //SMART : SLIM
                int minHorPointToCalculate = NUM_SLOTS;
                int maxHorPointToCalculate = 0;

                //int minVerPointToCalculate = (sensorVariant == 0) ? (NUM_LAYERS_0 - 1) : (NUM_LAYERS_1 - 1);  //SMART : SLIM
                int minVerPointToCalculate = NUM_LAYERS;
                int maxVerPointToCalculate = 0;
                for (int j = 0; j < snippetFolderContents_Real.Snippet_File_List[fileIndex].Snippet_Point_List.Count; j++)
                {
                    int cnt_SegHor = snippetFolderContents_Real.Snippet_File_List[fileIndex].Snippet_Point_List[j].pixelInLayer;
                    if (cnt_SegHor <= minHorPointToCalculate) minHorPointToCalculate = cnt_SegHor;
                    if (cnt_SegHor >= maxHorPointToCalculate) maxHorPointToCalculate = cnt_SegHor;

                    int cnt_SegVer = snippetFolderContents_Real.Snippet_File_List[fileIndex].Snippet_Point_List[j].layer;
                    if (cnt_SegVer <= minVerPointToCalculate) minVerPointToCalculate = cnt_SegVer;
                    if (cnt_SegVer >= maxVerPointToCalculate) maxVerPointToCalculate = cnt_SegVer;
                }

                snippet_file_Sim = GetSimulatedSnippetFile(snippetFolderContents_Real, fileIndex, (int)filesMerged.Value, fixNs, fixedSlantRange, fixSlantRange);

                Snippet_File limitedFOVSnippet = CreateLimitedFOVSnippet(snippet_file_Sim, minHorPointToCalculate, maxHorPointToCalculate, minVerPointToCalculate, maxVerPointToCalculate);

                accumulatedSnippet.Snippet_Point_List.AddRange(limitedFOVSnippet.Snippet_Point_List);

                WriteMergedSimulatedPCDFile(accumulatedSnippet);

                (float minArea, double minDistance) = CalculateMinAreaAndDistance(limitedFOVSnippet, snippet_file_Real);

                // SIM : Plot AREA histogram
                if (limitedFOVSnippet.Snippet_Point_List.Any())
                {
                    Histogram histo_Area_SIM = GetAreaHistogram(limitedFOVSnippet, limitedFOVSnippet.Snippet_Point_List.Min(p => p.AREA), limitedFOVSnippet.Snippet_Point_List.Max(p => p.AREA), 20);
                    UpdateChart("Sim", fileIndex, histo_Area_SIM, areaCharts);
                }
                else
                {
                    areaCharts[fileIndex].Series["Sim"].Points.Clear();
                }

                // SIM : Plot DISTANCE histogram
                if (limitedFOVSnippet.Snippet_Point_List.Any())
                {
                    Histogram histo_Dist_SIM = GetDistanceHistogram(limitedFOVSnippet, limitedFOVSnippet.Snippet_Point_List.Min(p => p.distance), limitedFOVSnippet.Snippet_Point_List.Max(p => p.distance), 20);
                    UpdateChart("Sim", fileIndex, histo_Dist_SIM, distanceCharts);
                }
                else
                {
                    distanceCharts[fileIndex].Series["Sim"].Points.Clear();
                }

                // SIM : Calculate Detection Probability
                CalculateDetectionProbability(fileIndex, limitedFOVSnippet, (int)filesMerged.Value, detectionProbabilityTextBoxes_Sim, false);

                // REAL : Plot AREA histogram
                Histogram histo_Area_Real = GetAreaHistogram(snippet_file_Real, snippet_file_Real.Snippet_Point_List.Min(p => p.AREA), snippet_file_Real.Snippet_Point_List.Max(p => p.AREA), 20);
                UpdateChart("Real", fileIndex, histo_Area_Real, areaCharts);

                // REAL : Plot DISTANCE histogram
                Histogram histo_Dist_Real = GetDistanceHistogram(snippet_file_Real, snippet_file_Real.Snippet_Point_List.Min(p => p.distance), snippet_file_Real.Snippet_Point_List.Max(p => p.distance), 20);
                UpdateChart("Real", fileIndex, histo_Dist_Real, distanceCharts);

                // Reset the Axis of the charts after plotting both SIM and REAL Area and Distance
                PCCommonFunctions.ResetAxesInChart(areaCharts[fileIndex], true, false, minArea);
                PCCommonFunctions.ResetAxesInChart(distanceCharts[fileIndex], true, false, minDistance);

                // Save the charts as images using the helper method
                SaveChartAsImage(areaCharts[fileIndex], $"Noise_AreaChart_{realFileNames[fileIndex]}.png");
                SaveChartAsImage(distanceCharts[fileIndex], $"Noise_DistanceChart_{realFileNames[fileIndex]}.png");

                // REAL : Calculate Detection Probability
                CalculateDetectionProbability(fileIndex, snippet_file_Real, (int)filesMerged.Value, detectionProbabilityTextBoxes_Real, true);
            }
        }

        // Filters the points from the given snippet file to include only those within the specified 
        // horizontal (minHorFOV to maxHorFOV) and vertical (minVerFOV to maxVerFOV) Field of View (FOV).
        // <returns>A new snippet file containing only points within the specified FOV
        Snippet_File CreateLimitedFOVSnippet(Snippet_File originalSnippet, int minHorFOV, int maxHorFOV, int minVerFOV, int maxVerFOV)
        {
            Snippet_File limitedSnippet = new Snippet_File
            {
                Snippet_Point_List = new List<Snippet_Point>()
            };

            foreach (var point in originalSnippet.Snippet_Point_List)
            {
                if (point.pixelInLayer >= minHorFOV && point.pixelInLayer <= maxHorFOV &&
                    point.layer >= minVerFOV && point.layer <= maxVerFOV)
                {
                    limitedSnippet.Snippet_Point_List.Add(point);
                }
            }

            return limitedSnippet;
        }

        // Generates a simulated snippet file by processing real snippet data with specified parameters.
        // Simulates the accumulation of multiple files and applies post-processing for analysis.
        public Snippet_File GetSimulatedSnippetFile(SnippetFolderContents snippetFolderContents_Real, int fileIndex, int noOfFilesMerged, double[] fixNs, double fixedSlantRange, bool fixSlantRange)
        {
            Snippet_File snippet_file_Sim = new Snippet_File();
            snippet_file_Sim.Snippet_Point_List = new List<Snippet_Point>(); // Initialize the list to hold simulated accumulated points when multiplied by 30.

            for (int i = 0; i < noOfFilesMerged; i++)
            {
                PostprocessingInput postprocessingInput = FillPostProcessingInputforInfiniteSurfaces(snippetFolderContents_Real.Snippet_File_List[fileIndex], fixNs, fixedSlantRange, fixSlantRange);
                PostprocessingOutput postprocessingOutput = ProcessScalaFrame(postprocessingInput, true, false);
                Snippet_File snippetFileFromOutput = GetSnippetFileFromPostprocessingOutput(postprocessingOutput);

                // Add the points from the current snippet file to the accumulated list.
                snippet_file_Sim.Snippet_Point_List.AddRange(snippetFileFromOutput.Snippet_Point_List);
            }

            return snippet_file_Sim;
        }

        public void WriteMergedSimulatedPCDFile(Snippet_File accumulatedSnippet)
        {
            if (checkBox_Statistics_writeOutputToFolder.Checked)
            {
                string completeFilePath = textBox_Statistics_OutputFolderPath.Text + "Merged_SIM.pcd";
                DebugBox.AppendText("writing snippet filename: " + completeFilePath + "\n");

                WriteOutputToPCD(accumulatedSnippet, completeFilePath);
            }
        }

        // Calculates the minimum area and minimum distance values between two snippet files.
        // Useful for setting chart axis limits and ensuring consistent comparisons between real and simulated data.

        public (float minArea, double minDistance) CalculateMinAreaAndDistance(Snippet_File limitedFOVSnippet, Snippet_File snippet_file_Real)
        {
            if(limitedFOVSnippet.Snippet_Point_List.Count == 0) return (0, 0);
            float minArea = Math.Min(limitedFOVSnippet.Snippet_Point_List.Min(p => p.AREA), snippet_file_Real.Snippet_Point_List.Min(p => p.AREA));
            double minDistance = Math.Min(limitedFOVSnippet.Snippet_Point_List.Min(p => p.distance), snippet_file_Real.Snippet_Point_List.Min(p => p.distance));

            return (minArea, minDistance);
        }

        private void UpdateChart(string seriesName, int fileIndex, Histogram histogram, Chart[] charts)
        {
            charts[fileIndex].Series[seriesName].Points.Clear();
            for (int i = 0; i < histogram.numberOfBins; i++)
            {
                charts[fileIndex].Series[seriesName].Points.AddXY(histogram.Xvalues[i], histogram.Yvalues[i] + 0.1);
            }
        }

        //Calculates the detection probability for a given snippet file (real or simulated) and updates the corresponding TextBox.
        private void CalculateDetectionProbability(int fileIndex, Snippet_File snippetFile, int noOfFilesMerged, TextBox[] detectionProbabilityTextBoxes, bool isRealFile)
        {
            if (snippetFile.Snippet_Point_List.Count == 0)
            {
                detectionProbabilityTextBoxes[fileIndex].Text = $"0%";
                return;
            }


            int minLayer = snippetFile.Snippet_Point_List.Min(p => p.layer);
            int maxLayer = snippetFile.Snippet_Point_List.Max(p => p.layer);
            int minPixelInLayer = snippetFile.Snippet_Point_List.Min(p => p.pixelInLayer);
            int maxPixelInLayer = snippetFile.Snippet_Point_List.Max(p => p.pixelInLayer);

            // Calculate the total expected points
            int detectedPoints = 0, totalExpectedPoints = 0;
            if (isRealFile == false) // simulated file
            {
                totalExpectedPoints = (maxLayer - minLayer + 1) * (maxPixelInLayer - minPixelInLayer + 1) * noOfFilesMerged;
                detectedPoints = snippetFile.Snippet_Point_List.Count;
            }

            else //real file
            {
                totalExpectedPoints = (maxLayer - minLayer + 1) * (maxPixelInLayer - minPixelInLayer + 1) * noOfFilesMerged; // Since 30 files are merged as input, this is a constant
                detectedPoints = snippetFile.Snippet_Point_List.Count;
            }

                // Calculate the detection probability
                double detectionProbability = (double)detectedPoints / totalExpectedPoints * 100;

            // Update the corresponding TextBox
            detectionProbabilityTextBoxes[fileIndex].Text = $"{detectionProbability:F2}%";
        }

        private void SaveChartAsImage(Chart chart, string fileName)
        {
            string folderPath = @"C:\Projects\Scala3_calibrator_DATA\Images";

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, fileName);
            chart.SaveImage(filePath, ChartImageFormat.Png);
        }

        private Vector VectorForRealData(SnippetFolderContents snippetFolderContents_Real)
        {
            Vector sumVector = new Vector(0, 0, 0); // Use Vector for simplicity
            int pointCount = 0;

            foreach (var file in snippetFolderContents_Real.Snippet_File_List)
            {
                foreach (var point in file.Snippet_Point_List)
                {
                    // Negate x, y, and z because now we want to get the vector from Point to the Sensor
                    sumVector.X += -point.x;
                    sumVector.Y += -point.y;
                    sumVector.Z += -point.z;
                    pointCount++;
                }
            }

            // Calculate the average
            if (pointCount > 0)
            {
                sumVector.X /= pointCount;
                sumVector.Y /= pointCount;
                sumVector.Z /= pointCount;
            }

            // Normalize the vector
            double magnitude = Math.Sqrt(sumVector.X * sumVector.X + sumVector.Y * sumVector.Y + sumVector.Z * sumVector.Z);
            if (magnitude > 0)
            {
                sumVector.X /= magnitude;
                sumVector.Y /= magnitude;
                sumVector.Z /= magnitude;
            }

            return sumVector;
        }
    }

    //Utility struct for vector operations
    public struct Vector
    {
        public double X, Y, Z;

        public Vector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
